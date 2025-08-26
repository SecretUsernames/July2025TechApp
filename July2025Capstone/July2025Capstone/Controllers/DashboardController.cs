using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using July2025Capstone.Data;
using July2025Capstone.Models;

namespace July2025Capstone.Controllers
{
    // [Authorize] // Temporarily commented out for testing
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardData>> GetDashboardSummary()
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                // Mock dashboard data - replace with actual database queries
                var dashboardData = new DashboardData
                {
                    BloodPressure = "118/76",
                    HeartRate = 68,
                    Glucose = 128,
                    Weight = 162,
                    LastUpload = "2h",
                    TotalRecords = 24,
                    AlertCount = 0
                };

                // Here you would typically:
                // 1. Query latest vitals from database
                // 2. Calculate trends and alerts
                // 3. Get upload statistics
                // 4. Generate personalized insights

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard summary");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("recent-activities")]
        public async Task<ActionResult<List<RecentActivity>>> GetRecentActivities()
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                // Mock recent activities - replace with actual database queries
                var activities = new List<RecentActivity>
                {
                    new RecentActivity
                    {
                        Id = "1",
                        Description = "Lab results uploaded",
                        Timestamp = DateTime.Now.AddHours(-2),
                        Type = "Upload"
                    },
                    new RecentActivity
                    {
                        Id = "2",
                        Description = "Insurance info updated",
                        Timestamp = DateTime.Now.AddDays(-1),
                        Type = "Update"
                    },
                    new RecentActivity
                    {
                        Id = "3",
                        Description = "Blood pressure recorded",
                        Timestamp = DateTime.Now.AddDays(-3),
                        Type = "Vitals"
                    }
                };

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent activities");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("vitals-trends")]
        public async Task<ActionResult<VitalsTrends>> GetVitalsTrends([FromQuery] int days = 30)
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                // Mock vitals trends data for charts
                var trends = new VitalsTrends
                {
                    BloodPressure = GenerateBPData(days),
                    HeartRate = GenerateHRData(days),
                    Glucose = GenerateGlucoseData(days),
                    Weight = GenerateWeightData(days)
                };

                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vitals trends");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("alerts")]
        public async Task<ActionResult<List<HealthAlert>>> GetHealthAlerts()
        {
            try
            {
                // Mock health alerts
                var alerts = new List<HealthAlert>
                {
                    // No alerts for now - user is healthy!
                };

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading health alerts");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helper methods to generate mock data
        private List<VitalReading> GenerateBPData(int days)
        {
            var random = new Random();
            var data = new List<VitalReading>();
            
            for (int i = days; i >= 0; i--)
            {
                data.Add(new VitalReading
                {
                    Date = DateTime.Now.AddDays(-i),
                    Systolic = random.Next(110, 130),
                    Diastolic = random.Next(70, 85)
                });
            }
            
            return data;
        }

        private List<VitalReading> GenerateHRData(int days)
        {
            var random = new Random();
            var data = new List<VitalReading>();
            
            for (int i = days; i >= 0; i--)
            {
                data.Add(new VitalReading
                {
                    Date = DateTime.Now.AddDays(-i),
                    Value = random.Next(60, 80)
                });
            }
            
            return data;
        }

        private List<VitalReading> GenerateGlucoseData(int days)
        {
            var random = new Random();
            var data = new List<VitalReading>();
            
            for (int i = days; i >= 0; i--)
            {
                data.Add(new VitalReading
                {
                    Date = DateTime.Now.AddDays(-i),
                    Value = random.Next(90, 140)
                });
            }
            
            return data;
        }

        private List<VitalReading> GenerateWeightData(int days)
        {
            var random = new Random();
            var baseWeight = 162;
            var data = new List<VitalReading>();
            
            for (int i = days; i >= 0; i--)
            {
                data.Add(new VitalReading
                {
                    Date = DateTime.Now.AddDays(-i),
                    Value = baseWeight + random.Next(-3, 4) // +/- 3 lbs variation
                });
            }
            
            return data;
        }
    }

    // Dashboard-specific models (only include unique ones)
    public class DashboardData
    {
        public string BloodPressure { get; set; } = string.Empty;
        public int HeartRate { get; set; }
        public int Glucose { get; set; }
        public int Weight { get; set; }
        public string LastUpload { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int AlertCount { get; set; }
    }

    public class VitalsTrends
    {
        public List<VitalReading> BloodPressure { get; set; } = new();
        public List<VitalReading> HeartRate { get; set; } = new();
        public List<VitalReading> Glucose { get; set; } = new();
        public List<VitalReading> Weight { get; set; } = new();
    }

    public class HealthAlert
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}