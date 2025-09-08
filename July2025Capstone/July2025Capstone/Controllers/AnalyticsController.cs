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
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var bloodPressureData = await _context.VitalBloodPressures
                    .Where(bp => bp.UserId == userId)
                    .OrderBy(bp => bp.DateMeasured)
                    .Take(30) // Last 30 readings
                    .ToListAsync();

                return Ok(bloodPressureData);
            }
            catch (Exception ex)
            {
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