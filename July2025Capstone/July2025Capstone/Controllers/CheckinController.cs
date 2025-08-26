using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using July2025Capstone.Data;
using System.Text.Json;

namespace July2025Capstone.Controllers
{
    // [Authorize] // Temporarily commented out for testing
    [ApiController]
    [Route("api/[controller]")]
    public class CheckinController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CheckinController> _logger;

        public CheckinController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CheckinController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<CheckinUploadResponse>> UploadCheckinProfile([FromForm] CheckinUploadRequest request)
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                var response = new CheckinUploadResponse
                {
                    Success = true,
                    Message = "Check-in profile uploaded successfully!",
                    ProfileId = Guid.NewGuid().ToString()
                };

                // Process file if provided
                if (request.File != null)
                {
                    _logger.LogInformation($"Processing file: {request.File.FileName}, Size: {request.File.Length} bytes");
                    
                    // Here you would typically:
                    // 1. Validate file type and size
                    // 2. Save file to storage (Azure Blob, local storage, etc.)
                    // 3. Parse and extract checkin data
                    // 4. Save to database
                    
                    response.Message += $" File '{request.File.FileName}' processed.";
                }

                // Process JSON data if provided
                if (!string.IsNullOrWhiteSpace(request.JsonData))
                {
                    try
                    {
                        // Validate JSON format
                        var jsonDoc = JsonDocument.Parse(request.JsonData);
                        _logger.LogInformation("JSON data validated and parsed successfully");
                        
                        // Here you would typically:
                        // 1. Validate JSON structure
                        // 2. Extract patient information
                        // 3. Save to database
                        
                        response.Message += " JSON data processed.";
                    }
                    catch (JsonException ex)
                    {
                        return BadRequest(new CheckinUploadResponse
                        {
                            Success = false,
                            Message = $"Invalid JSON format: {ex.Message}"
                        });
                    }
                }

                // Process notes if provided
                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    _logger.LogInformation("Notes added to checkin profile");
                    response.Message += " Notes saved.";
                }

                // TODO: Save to database
                // var checkinProfile = new CheckinProfile
                // {
                //     UserId = userId,
                //     JsonData = request.JsonData,
                //     Notes = request.Notes,
                //     FileName = request.File?.FileName,
                //     CreatedDate = DateTime.UtcNow
                // };
                // _context.CheckinProfiles.Add(checkinProfile);
                // await _context.SaveChangesAsync();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading checkin profile");
                return StatusCode(500, new CheckinUploadResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpGet("profiles")]
        public async Task<ActionResult<List<CheckinProfileSummary>>> GetCheckinProfiles()
        {
            try
            {
                // For testing, return mock data
                var profiles = new List<CheckinProfileSummary>
                {
                    new CheckinProfileSummary
                    {
                        Id = "1",
                        Name = "Primary Insurance Profile",
                        CreatedDate = DateTime.Now.AddDays(-7),
                        LastModified = DateTime.Now.AddDays(-2)
                    },
                    new CheckinProfileSummary
                    {
                        Id = "2",
                        Name = "Emergency Contact Info",
                        CreatedDate = DateTime.Now.AddDays(-14),
                        LastModified = DateTime.Now.AddDays(-5)
                    }
                };

                return Ok(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checkin profiles");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    // Model classes for API requests/responses
    public class CheckinUploadRequest
    {
        public IFormFile? File { get; set; }
        public string? JsonData { get; set; }
        public string? Notes { get; set; }
    }

    public class CheckinUploadResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ProfileId { get; set; }
    }

    public class CheckinProfileSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
    }
}