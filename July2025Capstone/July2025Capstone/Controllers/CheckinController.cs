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
    [Authorize] // Enable authorization for all actions
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

        private async Task<string?> GetCurrentUserIdAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return _userManager.GetUserId(User);
            }
            
            // For development: create or get a test user
            var testUser = await _userManager.FindByEmailAsync("test@example.com");
            if (testUser == null)
            {
                testUser = new ApplicationUser
                {
                    UserName = "test@example.com",
                    Email = "test@example.com",
                    EmailConfirmed = true
                };
                
                var result = await _userManager.CreateAsync(testUser, "Test123!");
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    return null;
                }
            }
            
            return testUser.Id;
        }

        [HttpGet("user-form")]
        public async Task<ActionResult<CheckinFormSummary>> GetUserForm()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

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
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                if (!int.TryParse(formId, out int patientId))
                {
                    return BadRequest("Invalid form ID");
                }

                // Get patient data with all related information - ensure it belongs to current user
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
                    .FirstOrDefaultAsync(p => p.Id == patientId && p.UserId == userId);

                if (patient == null)
                {
                    return NotFound("Patient form not found or access denied");
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
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Ensure the patient belongs to the current user
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
                    .FirstOrDefaultAsync(p => p.Id == patientId && p.UserId == userId);

                if (patient == null)
                {
                    return NotFound("Patient not found or access denied");
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
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

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
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Get all patients for the current user
                var patients = await _context.Patients
                    .Where(p => p.UserId == userId)
                    .Select(p => new CheckinProfileSummary
                    {
                        Id = p.Id.ToString(),
                        Name = $"{p.FirstName} {p.LastName} - Check-in Profile",
                        CreatedDate = DateTime.Now, // Add these fields to Patient model
                        LastModified = DateTime.Now
                    })
                    .ToListAsync();

                return Ok(patients);
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
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Check if patient already exists for this user
                var existingPatient = await _context.Patients
                    .Include(p => p.Address)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                Address? address = null;

                // Handle address if provided
                if (request.Address != null)
                {
                    if (existingPatient?.Address != null)
                    {
                        // Update existing address
                        address = existingPatient.Address;
                        address.Street = request.Address.Street;
                        address.City = request.Address.City;
                        address.State = request.Address.State;
                        address.PostalCode = request.Address.PostalCode;
                        address.Country = request.Address.Country;
                    }
                    else
                    {
                        // Create new address
                        address = new Address
                        {
                            Street = request.Address.Street,
                            City = request.Address.City,
                            State = request.Address.State,
                            PostalCode = request.Address.PostalCode,
                            Country = request.Address.Country
                        };
                        _context.Addresses.Add(address);
                        await _context.SaveChangesAsync(); // Save to get the address ID
                    }
                }

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
                    
                    if (address != null)
                    {
                        existingPatient.AddressId = address.Id;
                        existingPatient.Address = address;
                    }
                }
                else
                {
                    // Create new patient for this user
                    var newPatient = new Patient
                    {
                        UserId = userId,
                        FirstName = request.PersonalInfo.FirstName,
                        LastName = request.PersonalInfo.LastName,
                        DateOfBirth = request.PersonalInfo.DateOfBirth,
                        Gender = (int)request.PersonalInfo.Gender,
                        Email = request.PersonalInfo.Email,
                        Phone = request.PersonalInfo.Phone,
                        PreferredContactMethod = (int)request.PersonalInfo.PreferredContactMethod,
                        AddressId = address?.Id,
                        Address = address
                    };

                    _context.Patients.Add(newPatient);
                }

                await _context.SaveChangesAsync();

                var patientId = existingPatient?.Id ?? (await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId))?.Id ?? 0;

                return Ok(new SavePersonalInfoResponse
                {
                    Success = true,
                    Message = "Personal information saved successfully!",
                    PatientId = patientId
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
        public async Task<ActionResult<GetPersonalInfoResponse>> GetPersonalInfo()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .Include(p => p.Address)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return NotFound("No patient record found for current user");
                }

                var response = new GetPersonalInfoResponse
                {
                    PersonalInfo = new PersonalInfoDto
                    {
                        FirstName = patient.FirstName,
                        LastName = patient.LastName,
                        DateOfBirth = patient.DateOfBirth,
                        Gender = (GenderType)patient.Gender,
                        Email = patient.Email,
                        Phone = patient.Phone,
                        PreferredContactMethod = (ContactMethod)patient.PreferredContactMethod
                    },
                    Address = patient.Address != null ? new AddressDto
                    {
                        Street = patient.Address.Street,
                        City = patient.Address.City,
                        State = patient.Address.State,
                        PostalCode = patient.Address.PostalCode,
                        Country = patient.Address.Country
                    } : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal info");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("medications")]
        public async Task<ActionResult<List<MedicationDto>>> GetMedications()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return Ok(new List<MedicationDto>()); // Return empty list if no patient record
                }

                var medications = await _context.Medications
                    .Where(m => m.PatientId == patient.Id)
                    .ToListAsync();

                var medicationDtos = medications.Select(m => new MedicationDto
                {
                    Id = m.Id,
                    Name = m.Name ?? "",
                    DosageStrength = m.DosageStrength,
                    DosageUnit = (Shared.Models.DosageUnit)m.DosageUnit,
                    CustomDosageUnit = m.CustomDosageUnit,
                    Frequency = (Shared.Models.MedicationFrequency)m.Frequency
                }).ToList();

                return Ok(medicationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medications");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-medication")]
        public async Task<ActionResult<SaveMedicationResponse>> SaveMedication([FromBody] SaveMedicationRequest request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                // Get or create patient record
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return BadRequest("Patient record not found. Please complete personal information first.");
                }

                if (request.Medication.Id > 0)
                {
                    // Update existing medication
                    var existingMedication = await _context.Medications
                        .FirstOrDefaultAsync(m => m.Id == request.Medication.Id && m.PatientId == patient.Id);

                    if (existingMedication == null)
                    {
                        return NotFound("Medication not found or access denied");
                    }

                    existingMedication.Name = request.Medication.Name;
                    existingMedication.DosageStrength = request.Medication.DosageStrength;
                    existingMedication.DosageUnit = (Models.DosageUnit)request.Medication.DosageUnit;
                    existingMedication.CustomDosageUnit = request.Medication.CustomDosageUnit;
                    existingMedication.Frequency = (Models.MedicationFrequency)request.Medication.Frequency;
                }
                else
                {
                    // Create new medication
                    var newMedication = new Medication
                    {
                        PatientId = patient.Id,
                        Name = request.Medication.Name,
                        DosageStrength = request.Medication.DosageStrength,
                        DosageUnit = (Models.DosageUnit)request.Medication.DosageUnit,
                        CustomDosageUnit = request.Medication.CustomDosageUnit,
                        Frequency = (Models.MedicationFrequency)request.Medication.Frequency
                    };

                    _context.Medications.Add(newMedication);
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveMedicationResponse
                {
                    Success = true,
                    Message = "Medication saved successfully!",
                    MedicationId = request.Medication.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving medication");
                return StatusCode(500, new SaveMedicationResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpDelete("medication/{medicationId}")]
        public async Task<ActionResult<DeleteMedicationResponse>> DeleteMedication(int medicationId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return BadRequest("Patient record not found");
                }

                var medication = await _context.Medications
                    .FirstOrDefaultAsync(m => m.Id == medicationId && m.PatientId == patient.Id);

                if (medication == null)
                {
                    return NotFound("Medication not found or access denied");
                }

                _context.Medications.Remove(medication);
                await _context.SaveChangesAsync();

                return Ok(new DeleteMedicationResponse
                {
                    Success = true,
                    Message = "Medication deleted successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting medication");
                return StatusCode(500, new DeleteMedicationResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("allergies")]
        public async Task<ActionResult<List<AllergyDto>>> GetAllergies()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return Ok(new List<AllergyDto>()); // Return empty list if no patient record
                }

                var allergies = await _context.Allergies
                    .Where(a => a.PatientId == patient.Id)
                    .ToListAsync();

                var allergyDtos = allergies.Select(a => new AllergyDto
                {
                    Id = a.Id,
                    Allergen = a.Allergen
                }).ToList();

                return Ok(allergyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving allergies");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-allergy")]
        public async Task<ActionResult<SaveAllergyResponse>> SaveAllergy([FromBody] SaveAllergyRequest request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                // Get or create patient record
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return BadRequest("Patient record not found. Please complete personal information first.");
                }

                if (request.Allergy.Id > 0)
                {
                    // Update existing allergy
                    var existingAllergy = await _context.Allergies
                        .FirstOrDefaultAsync(a => a.Id == request.Allergy.Id && a.PatientId == patient.Id);

                    if (existingAllergy == null)
                    {
                        return NotFound("Allergy not found or access denied");
                    }

                    existingAllergy.Allergen = request.Allergy.Allergen;
                }
                else
                {
                    // Create new allergy
                    var newAllergy = new Allergy
                    {
                        PatientId = patient.Id,
                        Allergen = request.Allergy.Allergen
                    };

                    _context.Allergies.Add(newAllergy);
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveAllergyResponse
                {
                    Success = true,
                    Message = "Allergy saved successfully!",
                    AllergyId = request.Allergy.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving allergy");
                return StatusCode(500, new SaveAllergyResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpDelete("allergy/{allergyId}")]
        public async Task<ActionResult<DeleteAllergyResponse>> DeleteAllergy(int allergyId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return BadRequest("Patient record not found");
                }

                var allergy = await _context.Allergies
                    .FirstOrDefaultAsync(a => a.Id == allergyId && a.PatientId == patient.Id);

                if (allergy == null)
                {
                    return NotFound("Allergy not found or access denied");
                }

                _context.Allergies.Remove(allergy);
                await _context.SaveChangesAsync();

                return Ok(new DeleteAllergyResponse
                {
                    Success = true,
                    Message = "Allergy deleted successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting allergy");
                return StatusCode(500, new DeleteAllergyResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
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