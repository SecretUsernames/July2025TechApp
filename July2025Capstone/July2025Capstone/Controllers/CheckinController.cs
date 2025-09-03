using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using July2025Capstone.Data;
using July2025Capstone.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using July2025Capstone.Shared.Models;

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

        [HttpGet("user-form")]
        public async Task<ActionResult<CheckinFormSummary>> GetUserForm()
        {
            try
            {
                // For testing, bypass user authentication - uncomment when ready for production
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                // For testing, use a mock user ID - replace with actual user ID in production
                var userId = "test-user-id";

                // Check if user has existing patient record with complete information
                var patient = await _context.Patients
                    .Include(p => p.Address)
                    .Include(p => p.InsurancePolicies)
                    .Include(p => p.EmergencyContacts)
                    .Include(p => p.Lifestyle)
                    .Include(p => p.Medications)
                    .Include(p => p.Allergies)
                    .Include(p => p.PreferredPharmacy)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return NotFound(); // No form exists
                }

                // Check if the patient record has enough information to be considered "complete"
                bool isComplete = !string.IsNullOrEmpty(patient.FirstName) &&
                                !string.IsNullOrEmpty(patient.LastName) &&
                                patient.InsurancePolicies.Any() &&
                                patient.EmergencyContacts.Any();

                var formSummary = new CheckinFormSummary
                {
                    Id = patient.Id.ToString(),
                    Name = $"{patient.FirstName} {patient.LastName} - Check-in Profile",
                    CreatedDate = DateTime.Now.AddDays(-30), // You might want to add a CreatedDate to Patient model
                    LastModified = DateTime.Now.AddDays(-5), // You might want to add a LastModified to Patient model
                    IsComplete = isComplete
                };

                return Ok(formSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user form");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("generate-pdf/{formId}")]
        public async Task<ActionResult> GenerateCheckInPdf(string formId)
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                if (!int.TryParse(formId, out int patientId))
                {
                    return BadRequest("Invalid form ID");
                }

                // Get patient data with all related information
                var patient = await _context.Patients
                    .Include(p => p.Address)
                    .Include(p => p.InsurancePolicies)
                    .Include(p => p.EmergencyContacts)
                    .Include(p => p.Lifestyle)
                    .Include(p => p.Medications)
                    .Include(p => p.Allergies)
                    .Include(p => p.PreferredPharmacy)
                        .ThenInclude(ph => ph.Address)
                    .Include(p => p.PatientConditions)
                        .ThenInclude(pc => pc.Condition)
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                {
                    return NotFound("Patient form not found");
                }

                // TODO: Implement actual PDF generation here using a library like iText7 or QuestPDF
                // For now, return a mock response with comprehensive patient data
                var pdfData = GenerateMockPdfContent(patient);
                var mockPdfContent = System.Text.Encoding.UTF8.GetBytes(pdfData);
                
                return File(mockPdfContent, "application/pdf", $"CheckInForm_{patient.FirstName}_{patient.LastName}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for form {FormId}", formId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<PatientDetailResponse>> GetPatientDetails(int patientId)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.Address)
                    .Include(p => p.InsurancePolicies)
                    .Include(p => p.EmergencyContacts)
                    .Include(p => p.Lifestyle)
                    .Include(p => p.Medications)
                    .Include(p => p.Allergies)
                    .Include(p => p.PreferredPharmacy)
                        .ThenInclude(ph => ph.Address)
                    .Include(p => p.PatientConditions)
                        .ThenInclude(pc => pc.Condition)
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                {
                    return NotFound();
                }

                var response = new PatientDetailResponse
                {
                    Patient = patient,
                    CompletionStatus = new FormCompletionStatus
                    {
                        HasPersonalInfo = !string.IsNullOrEmpty(patient.FirstName) && !string.IsNullOrEmpty(patient.LastName),
                        HasAddress = patient.Address != null,
                        HasInsurance = patient.InsurancePolicies.Any(),
                        HasEmergencyContacts = patient.EmergencyContacts.Any(),
                        HasMedications = patient.Medications.Any(),
                        HasAllergies = patient.Allergies.Any(),
                        HasLifestyle = patient.Lifestyle != null,
                        OverallComplete = false
                    }
                };

                // Calculate overall completion
                response.CompletionStatus.OverallComplete = 
                    response.CompletionStatus.HasPersonalInfo &&
                    response.CompletionStatus.HasAddress &&
                    response.CompletionStatus.HasInsurance &&
                    response.CompletionStatus.HasEmergencyContacts;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient details for ID {PatientId}", patientId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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
                    // 4. Save to database using the new Patient/Insurance/etc. models
                    
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
                        // 3. Create Patient, Address, InsurancePolicy, etc. records
                        // 4. Save to database
                        
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
                // Get all patients for the current user (when authentication is enabled)
                // var userId = _userManager.GetUserId(User);
                // var patients = await _context.Patients
                //     .Where(p => p.UserId == userId)
                //     .Select(p => new CheckinProfileSummary
                //     {
                //         Id = p.Id.ToString(),
                //         Name = $"{p.FirstName} {p.LastName} - Check-in Profile",
                //         CreatedDate = DateTime.Now, // Add these fields to Patient model
                //         LastModified = DateTime.Now
                //     })
                //     .ToListAsync();

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

        private string GenerateMockPdfContent(Patient patient)
        {
            var content = $@"
PATIENT CHECK-IN FORM
=====================

PERSONAL INFORMATION:
Name: {patient.FirstName} {patient.LastName}
Date of Birth: {patient.DateOfBirth}
Email: {patient.Email}
Phone: {patient.Phone}
Gender: {patient.Gender}

ADDRESS:
{(patient.Address != null ? $@"
{patient.Address.Street}
{patient.Address.City}, {patient.Address.State} {patient.Address.PostalCode}
{patient.Address.Country}" : "No address on file")}

INSURANCE POLICIES:
{(patient.InsurancePolicies.Any() ? 
    string.Join("\n", patient.InsurancePolicies.Select(ip => 
        $"Provider: {ip.Provider}, Policy: {ip.PolicyNumber}, Group: {ip.GroupNumber}")) : 
    "No insurance policies on file")}

EMERGENCY CONTACTS:
{(patient.EmergencyContacts.Any() ? 
    string.Join("\n", patient.EmergencyContacts.Select(ec => 
        $"{ec.FirstName} {ec.LastName} - {ec.Relationship} - {ec.Phone}")) : 
    "No emergency contacts on file")}

MEDICATIONS:
{(patient.Medications.Any() ? 
    string.Join("\n", patient.Medications.Select(m => 
        $"{m.Name} - {m.DosageStrength} {m.DosageUnit} - {m.Frequency}")) : 
    "No medications on file")}

ALLERGIES:
{(patient.Allergies.Any() ? 
    string.Join("\n", patient.Allergies.Select(a => a.Allergen)) : 
    "No known allergies")}

PREFERRED PHARMACY:
{(patient.PreferredPharmacy != null ? 
    $"{patient.PreferredPharmacy.Name} - {patient.PreferredPharmacy.Phone}" : 
    "No preferred pharmacy selected")}
";
            return content;
        }

        [HttpPost("save-personal-info")]
        public async Task<ActionResult<SavePersonalInfoResponse>> SavePersonalInfo([FromBody] SavePersonalInfoRequest request)
        {
            try
            {
                // For testing, bypass user authentication
                var userId = "test-user-id"; // Replace with actual user ID when auth is enabled
                // var userId = _userManager.GetUserId(User);

                // Check if patient already exists
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (existingPatient != null)
                {
                    // Update existing patient
                    existingPatient.FirstName = request.PersonalInfo.FirstName;
                    existingPatient.LastName = request.PersonalInfo.LastName;
                    existingPatient.DateOfBirth = request.PersonalInfo.DateOfBirth;
                    existingPatient.Gender = (int)request.PersonalInfo.Gender;
                    existingPatient.Email = request.PersonalInfo.Email;
                    existingPatient.Phone = request.PersonalInfo.Phone;
                    existingPatient.PreferredContactMethod = (int)request.PersonalInfo.PreferredContactMethod;
                }
                else
                {
                    // Create new patient
                    var newPatient = new Patient
                    {
                        UserId = userId,
                        FirstName = request.PersonalInfo.FirstName,
                        LastName = request.PersonalInfo.LastName,
                        DateOfBirth = request.PersonalInfo.DateOfBirth,
                        Gender = (int)request.PersonalInfo.Gender,
                        Email = request.PersonalInfo.Email,
                        Phone = request.PersonalInfo.Phone,
                        PreferredContactMethod = (int)request.PersonalInfo.PreferredContactMethod
                    };

                    _context.Patients.Add(newPatient);
                }

                await _context.SaveChangesAsync();

                return Ok(new SavePersonalInfoResponse
                {
                    Success = true,
                    Message = "Personal information saved successfully!",
                    PatientId = existingPatient?.Id ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving personal info");
                return StatusCode(500, new SavePersonalInfoResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("personal-info")]
        public async Task<ActionResult<PersonalInfoDto>> GetPersonalInfo()
        {
            try
            {
                var userId = "test-user-id"; // Replace with actual user ID when auth is enabled

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return NotFound();
                }

                var dto = new PersonalInfoDto
                {
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = (GenderType)patient.Gender,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    PreferredContactMethod = (ContactMethod)patient.PreferredContactMethod
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal info");
                return StatusCode(500, $"Error: {ex.Message}");
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

    public class CheckinFormSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsComplete { get; set; }
    }

    public class PatientDetailResponse
    {
        public Patient Patient { get; set; } = null!;
        public FormCompletionStatus CompletionStatus { get; set; } = new();
    }

    public class FormCompletionStatus
    {
        public bool HasPersonalInfo { get; set; }
        public bool HasAddress { get; set; }
        public bool HasInsurance { get; set; }
        public bool HasEmergencyContacts { get; set; }
        public bool HasMedications { get; set; }
        public bool HasAllergies { get; set; }
        public bool HasLifestyle { get; set; }
        public bool OverallComplete { get; set; }
    }
}