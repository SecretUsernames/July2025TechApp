using July2025Capstone.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using July2025Capstone.Models;
using July2025Capstone.Shared.Models;

namespace July2025Capstone.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MedicationDoseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MedicationDoseController> _logger;

        public MedicationDoseController(ApplicationDbContext context, ILogger<MedicationDoseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/MedicationDose/medication/{medicationId}
        [HttpGet("medication/{medicationId}")]
        public async Task<ActionResult<List<MedicationDose>>> GetDosesForMedication(int medicationId)
        {
            var doses = await _context.MedicationDoses
                .Where(d => d.MedicationId == medicationId)
                .ToListAsync();

            return Ok(doses);
        }

        // Helper method to get times for frequency - using shared enum
        private static List<TimeOfDay> GetTimesForFrequency(Shared.Models.MedicationFrequency frequency)
        {
            return frequency switch
            {
                Shared.Models.MedicationFrequency.OnceDaily => new List<TimeOfDay> { TimeOfDay.Morning },
                Shared.Models.MedicationFrequency.TwiceDaily => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Evening },
                Shared.Models.MedicationFrequency.ThreeDaily => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening },
                Shared.Models.MedicationFrequency.FourDaily => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening, TimeOfDay.Bedtime },
                Shared.Models.MedicationFrequency.AsNeeded => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening, TimeOfDay.Bedtime },
                _ => new List<TimeOfDay>()
            };
        }

        // POST: api/MedicationDose/initialize-missing
        [HttpPost("initialize-missing")]
        public async Task<IActionResult> InitializeMissingDoses()
        {
            try
            {
                // Get all medications without checking user - the medication records already ensure user isolation
                var medications = await _context.Medications.ToListAsync();
                
                var createdDoses = 0;

                foreach (var medication in medications)
                {
                    var medicationFrequency = (Shared.Models.MedicationFrequency)medication.Frequency;
                    var times = GetTimesForFrequency(medicationFrequency);

                    for (int day = 0; day < 7; day++) // Explicitly 0-6 for Sun-Sat
                    {
                        foreach (var time in times)
                        {
                            // Check if dose already exists
                            var existingDose = await _context.MedicationDoses
                                .FirstOrDefaultAsync(d => 
                                    d.MedicationId == medication.Id &&
                                    d.DayOfWeek == day &&
                                    d.TimeOfDay == time);

                            if (existingDose == null)
                            {
                                // Validate day range before creating
                                if (day < 0 || day > 6)
                                {
                                    _logger.LogError("Invalid day value in loop: {Day}. Skipping.", day);
                                    continue;
                                }

                                // Create missing dose
                                var newDose = new Models.MedicationDose
                                {
                                    MedicationId = medication.Id,
                                    DayOfWeek = day,
                                    TimeOfDay = time,
                                    Taken = false,
                                    TakenAt = null
                                };

                                _context.MedicationDoses.Add(newDose);
                                createdDoses++;
                                
                                _logger.LogDebug("Created dose: MedicationId={MedicationId}, Day={Day}, Time={Time}", 
                                    medication.Id, day, (int)time);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {CreatedDoses} missing medication doses", createdDoses);

                return Ok(new { Success = true, CreatedDoses = createdDoses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing missing doses");
                return StatusCode(500, "Error initializing missing doses");
            }
        }

        // POST: api/MedicationDose/toggle
        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleDose([FromBody] ToggleDoseRequest request)
        {
            try
            {
                // Validate DayOfWeek range
                if (request.DayOfWeek < 0 || request.DayOfWeek > 6)
                {
                    _logger.LogError("Invalid DayOfWeek value: {DayOfWeek}. Must be 0-6.", request.DayOfWeek);
                    return BadRequest($"Invalid DayOfWeek value: {request.DayOfWeek}. Must be 0-6 (Sunday=0, Saturday=6).");
                }

                _logger.LogInformation("Toggle dose request: MedicationId={MedicationId}, DayOfWeek={DayOfWeek}, TimeOfDay={TimeOfDay}", 
                    request.MedicationId, request.DayOfWeek, (int)request.TimeOfDay);

                var dose = await _context.MedicationDoses.FirstOrDefaultAsync(d =>
                    d.MedicationId == request.MedicationId &&
                    d.DayOfWeek == request.DayOfWeek &&
                    d.TimeOfDay == request.TimeOfDay);

                if (dose == null)
                {
                    _logger.LogWarning("Dose not found, creating new dose for MedicationId={MedicationId}, DayOfWeek={DayOfWeek}, TimeOfDay={TimeOfDay}", 
                        request.MedicationId, request.DayOfWeek, (int)request.TimeOfDay);

                    // Create the dose if it doesn't exist
                    dose = new Models.MedicationDose
                    {
                        MedicationId = request.MedicationId,
                        DayOfWeek = request.DayOfWeek,
                        TimeOfDay = request.TimeOfDay,
                        Taken = true, // Set to true since user is trying to mark it as taken
                        TakenAt = DateTime.UtcNow
                    };

                    _context.MedicationDoses.Add(dose);
                }
                else
                {
                    // Toggle existing dose
                    dose.Taken = !dose.Taken;
                    dose.TakenAt = dose.Taken ? DateTime.UtcNow : null;
                    
                    _logger.LogInformation("Toggled dose: MedicationId={MedicationId}, DayOfWeek={DayOfWeek}, TimeOfDay={TimeOfDay}, Taken={Taken}", 
                        request.MedicationId, request.DayOfWeek, (int)request.TimeOfDay, dose.Taken);
                }

                await _context.SaveChangesAsync();

                return Ok(new { Success = true, Taken = dose.Taken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling dose for MedicationId={MedicationId}, DayOfWeek={DayOfWeek}, TimeOfDay={TimeOfDay}", 
                    request.MedicationId, request.DayOfWeek, (int)request.TimeOfDay);
                return StatusCode(500, "Error toggling dose");
            }
        }

        // POST: api/MedicationDose/reset-doses - TEMPORARY ENDPOINT TO FIX DATA
        [HttpPost("reset-doses")]
        public async Task<IActionResult> ResetAllDoses()
        {
            try
            {
                // Delete all existing doses (they have wrong enum values)
                var allDoses = await _context.MedicationDoses.ToListAsync();
                _context.MedicationDoses.RemoveRange(allDoses);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted {Count} existing doses with wrong enum values", allDoses.Count);

                // Now reinitialize with correct values
                var result = await InitializeMissingDoses();

                return Ok(new { Success = true, Message = "All doses reset and reinitialized with correct values" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting doses");
                return StatusCode(500, "Error resetting doses");
            }
        }

        // POST: api/MedicationDose/cleanup-invalid-days - TEMPORARY ENDPOINT TO FIX INVALID DAY VALUES
        [HttpPost("cleanup-invalid-days")]
        public async Task<IActionResult> CleanupInvalidDays()
        {
            try
            {
                // Find and delete any doses with invalid DayOfWeek values (should be 0-6)
                var invalidDoses = await _context.MedicationDoses
                    .Where(d => d.DayOfWeek < 0 || d.DayOfWeek > 6)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} doses with invalid DayOfWeek values", invalidDoses.Count);

                if (invalidDoses.Any())
                {
                    _context.MedicationDoses.RemoveRange(invalidDoses);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Deleted {Count} doses with invalid DayOfWeek values", invalidDoses.Count);
                }

                return Ok(new { 
                    Success = true, 
                    Message = $"Cleaned up {invalidDoses.Count} doses with invalid DayOfWeek values",
                    DeletedCount = invalidDoses.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up invalid days");
                return StatusCode(500, "Error cleaning up invalid days");
            }
        }
    }
}
