using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using July2025Capstone.Data;
using July2025Capstone.Models;

namespace July2025Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AnalyticsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("recent-uploads")]
        public async Task<ActionResult<List<UploadItem>>> GetRecentUploads()
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                // Mock data - replace with actual database queries
                var uploads = new List<UploadItem>
                {
                    new UploadItem { Name = "CBC Panel", TimeAgo = "2 hours ago", UploadDate = DateTime.Now.AddHours(-2) },
                    new UploadItem { Name = "MRI Report", TimeAgo = "Yesterday", UploadDate = DateTime.Now.AddDays(-1) },
                    new UploadItem { Name = "Insurance Card", TimeAgo = "3 days ago", UploadDate = DateTime.Now.AddDays(-3) }
                };

                return Ok(uploads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("vitals/blood-pressure")]
        public async Task<ActionResult<List<VitalBloodPressure>>> GetBloodPressureData()
        {
            try
            {
                _logger.LogInformation("GetBloodPressureData called");
                // Get both ID and email
                var userId = _userManager.GetUserId(User);
                var userEmail = User.Identity?.Name;
                
                _logger.LogInformation($"User ID from UserManager: {userId}");
                _logger.LogInformation($"User Email from Claims: {userEmail}");
                _logger.LogInformation($"Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");

                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("User email is null or empty");
                    return Unauthorized();
                }

                // Query using user ID
                var bloodPressureData = await _context.VitalBloodPressures
                    .Where(bp => bp.UserId == userId)
                    .OrderBy(bp => bp.DateMeasured)
                    .Take(30) // Last 30 readings
                    .ToListAsync();

                _logger.LogInformation($"Found {bloodPressureData.Count} blood pressure readings for user {userId}");
                
                // Log the first few readings if any exist
                if (bloodPressureData.Any())
                {
                    _logger.LogInformation("Sample readings: " + 
                        string.Join(", ", bloodPressureData.Take(3)
                            .Select(bp => $"{{Id={bp.Id}, Systolic={bp.Systolic}, Diastolic={bp.Diastolic}}}")));
                }

                return Ok(bloodPressureData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetBloodPressureData: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("vitals/glucose")]
        public async Task<ActionResult<List<VitalGlucose>>> GetGlucoseData()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var glucoseData = await _context.VitalGlucoses
                    .Where(g => g.UserId == userId)
                    .OrderBy(g => g.DateMeasured)
                    .Take(30) // Last 30 readings
                    .ToListAsync();

                return Ok(glucoseData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("vitals/blood-pressure")]
        public async Task<ActionResult<VitalBloodPressure>> AddBloodPressureReading([FromBody] VitalBloodPressure reading)
        {
            try
            {
                _logger.LogInformation($"Received blood pressure reading: Systolic={reading.Systolic}, Diastolic={reading.Diastolic}");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid: " + string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not authenticated");
                    return Unauthorized();
                }

                _logger.LogInformation($"User ID from UserManager: {userId}");

                reading.UserId = userId;
                _context.VitalBloodPressures.Add(reading);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully saved blood pressure reading with ID: {reading.Id}");
                return Ok(reading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving blood pressure reading: {ex}");
                return StatusCode(500, "Internal server error while saving blood pressure reading");
            }
        }

        [HttpDelete("vitals/blood-pressure/{id}")]
        public async Task<ActionResult> DeleteBloodPressureReading(int id)
        {
            try
            {
                _logger.LogInformation($"Chart refresh triggered for user {_userManager.GetUserId(User)}");
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var reading = await _context.VitalBloodPressures
                    .FirstOrDefaultAsync(bp => bp.Id == id && bp.UserId == userId);

                if (reading == null)
                {
                    return NotFound();
                }

                _context.VitalBloodPressures.Remove(reading);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully deleted blood pressure reading with ID: {id}");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting blood pressure reading: {ex}");
                return StatusCode(500, "Internal server error while deleting blood pressure reading");
            }
        }

        [HttpPost("vitals/glucose")]
        public async Task<ActionResult<VitalGlucose>> AddGlucoseReading([FromBody] VitalGlucose reading)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Create a new entity and copy only the needed properties
                var newReading = new VitalGlucose
                {
                    UserId = userId,
                    GlucoseValue = reading.GlucoseValue,
                    DateMeasured = reading.DateMeasured
                };

                _context.VitalGlucoses.Add(newReading);
                await _context.SaveChangesAsync();

                return Ok(newReading);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("vitals/weight")]
        public async Task<ActionResult<VitalWeight>> AddWeightReading([FromBody] VitalWeight reading)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Create a new entity and copy only the needed properties
                var newReading = new VitalWeight
                {
                    UserId = userId,
                    WeightValue = reading.WeightValue,
                    Unit = reading.Unit,
                    DateMeasured = reading.DateMeasured
                };

                _context.VitalWeights.Add(newReading);
                await _context.SaveChangesAsync();

                return Ok(newReading);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("vitals/weight")]
        public async Task<ActionResult<List<VitalWeight>>> GetWeightData()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var weightData = await _context.VitalWeights
                    .Where(w => w.UserId == userId)
                    .OrderBy(w => w.DateMeasured)
                    .Take(30) // Last 30 readings
                    .ToListAsync();

                return Ok(weightData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("alerts")]
        public async Task<ActionResult<AlertsResponse>> GetAlerts()
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                // Mock data - replace with actual database queries
                var response = new AlertsResponse
                {
                    HasAlerts = false,
                    Count = 0,
                    Alerts = new List<Alert>()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("vitals")]
        public async Task<ActionResult<VitalsData>> GetVitalsData()
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                // Mock vitals data
                var vitalsData = new VitalsData
                {
                    Readings = new List<VitalReading>
                    {
                        new VitalReading { Date = DateTime.Now.AddDays(-7), Type = "Blood Pressure", Value = 120, Unit = "mmHg" },
                        new VitalReading { Date = DateTime.Now.AddDays(-6), Type = "Blood Pressure", Value = 118, Unit = "mmHg" },
                        new VitalReading { Date = DateTime.Now.AddDays(-5), Type = "Blood Pressure", Value = 122, Unit = "mmHg" }
                    }
                };

                return Ok(vitalsData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("vitals/glucose/{id}")]
        public async Task<ActionResult> DeleteGlucoseReading(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var reading = await _context.VitalGlucoses
                    .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

                if (reading == null)
                {
                    return NotFound();
                }

                _context.VitalGlucoses.Remove(reading);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully deleted glucose reading with ID: {id}");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting glucose reading: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("vitals/weight/{id}")]
        public async Task<ActionResult> DeleteWeightReading(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var reading = await _context.VitalWeights
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

                if (reading == null)
                {
                    return NotFound();
                }

                _context.VitalWeights.Remove(reading);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully deleted weight reading with ID: {id}");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting weight reading: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT endpoints for updating readings
        [HttpPut("vitals/blood-pressure/{id}")]
        public async Task<ActionResult<VitalBloodPressure>> UpdateBloodPressureReading(int id, [FromBody] VitalBloodPressure reading)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                _logger.LogInformation($"Updating blood pressure reading with ID: {id}");

                // Find the existing reading
                var existingReading = await _context.VitalBloodPressures
                    .FirstOrDefaultAsync(bp => bp.Id == id && bp.UserId == userId);

                if (existingReading == null)
                {
                    return NotFound();
                }

                // Update the properties
                existingReading.Systolic = reading.Systolic;
                existingReading.Diastolic = reading.Diastolic;
                existingReading.DateMeasured = reading.DateMeasured;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully updated blood pressure reading with ID: {id}");
                return Ok(existingReading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating blood pressure reading: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("vitals/glucose/{id}")]
        public async Task<ActionResult<VitalGlucose>> UpdateGlucoseReading(int id, [FromBody] VitalGlucose reading)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                _logger.LogInformation($"Updating glucose reading with ID: {id}");

                // Find the existing reading
                var existingReading = await _context.VitalGlucoses
                    .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

                if (existingReading == null)
                {
                    return NotFound();
                }

                // Update the properties
                existingReading.GlucoseValue = reading.GlucoseValue;
                existingReading.DateMeasured = reading.DateMeasured;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully updated glucose reading with ID: {id}");
                return Ok(existingReading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating glucose reading: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("vitals/weight/{id}")]
        public async Task<ActionResult<VitalWeight>> UpdateWeightReading(int id, [FromBody] VitalWeight reading)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                _logger.LogInformation($"Updating weight reading with ID: {id}");

                // Find the existing reading
                var existingReading = await _context.VitalWeights
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

                if (existingReading == null)
                {
                    return NotFound();
                }

                // Update the properties
                existingReading.WeightValue = reading.WeightValue;
                existingReading.Unit = reading.Unit;
                existingReading.DateMeasured = reading.DateMeasured;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully updated weight reading with ID: {id}");
                return Ok(existingReading);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating weight reading: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    // Keep only the response wrapper classes here
    public class AlertsResponse
    {
        public bool HasAlerts { get; set; }
        public int Count { get; set; }
        public List<Alert> Alerts { get; set; } = new();
    }

    public class VitalsData
    {
        public List<VitalReading> Readings { get; set; } = new();
    }
}