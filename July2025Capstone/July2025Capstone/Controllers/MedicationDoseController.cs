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

        // POST: api/MedicationDose/toggle
        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleDose([FromBody] ToggleDoseRequest request)
        {
            var dose = await _context.MedicationDoses.FirstOrDefaultAsync(d =>
                d.MedicationId == request.MedicationId &&
                d.DayOfWeek == request.DayOfWeek &&
                d.TimeOfDay == request.TimeOfDay);

            if (dose == null)
            {
                return NotFound("Dose not found");
            }

            dose.Taken = !dose.Taken;
            dose.TakenAt = dose.Taken ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Taken = dose.Taken });
        }
    }
}
