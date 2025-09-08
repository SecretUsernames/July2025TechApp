using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using July2025Capstone.Data;
using July2025Capstone.Models;
using July2025Capstone.Services;
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
        private readonly IPdfGenerationService _pdfService;

        public CheckinController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CheckinController> logger, IPdfGenerationService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _pdfService = pdfService;
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
                    .Include(p => p.VisitIntakes)
                    .Include(p => p.Consent)
                    .Include(p => p.PreferredPharmacy)
                        .ThenInclude(ph => ph.Address)
                    .Include(p => p.PatientConditions)
                        .ThenInclude(pc => pc.Condition)
                    .FirstOrDefaultAsync(p => p.Id == patientId && p.UserId == userId);

                if (patient == null)
                {
                    return NotFound("Patient form not found or access denied");
                }

                // Validate that all required sections are completed before generating PDF
                var validationErrors = new List<string>();
                
                if (string.IsNullOrEmpty(patient.FirstName) || string.IsNullOrEmpty(patient.LastName))
                {
                    validationErrors.Add("Personal information is incomplete");
                }
                
                if (patient.Address == null)
                {
                    validationErrors.Add("Address information is missing");
                }
                
                if (!patient.InsurancePolicies.Any())
                {
                    validationErrors.Add("Insurance information is missing");
                }
                
                if (!patient.EmergencyContacts.Any())
                {
                    validationErrors.Add("Emergency contacts are missing");
                }
                
                if (patient.Lifestyle == null)
                {
                    validationErrors.Add("Lifestyle information is missing");
                }
                
                if (!patient.VisitIntakes.Any())
                {
                    validationErrors.Add("Reason for visit is missing");
                }
                
                if (patient.Consent == null)
                {
                    validationErrors.Add("Consent is required");
                }

                if (validationErrors.Any())
                {
                    return BadRequest($"Cannot generate PDF. Please complete the following required sections: {string.Join(", ", validationErrors)}");
                }

                // Generate the actual PDF using QuestPDF
                var pdfBytes = _pdfService.GenerateCheckInPdf(patient);
                
                return File(pdfBytes, "application/pdf", $"CheckInForm_{patient.FirstName}_{patient.LastName}_{DateTime.Now:yyyyMMdd}.pdf");
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
                    .Include(p => p.VisitIntakes)
                    .Include(p => p.Consent)
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
                    CompletionStatus = new FormCompletionStatus
                    {
                        HasPersonalInfo = !string.IsNullOrEmpty(patient.FirstName) && !string.IsNullOrEmpty(patient.LastName),
                        HasAddress = patient.Address != null,
                        HasInsurance = patient.InsurancePolicies.Any(),
                        HasEmergencyContacts = patient.EmergencyContacts.Any(),
                        HasMedications = patient.Medications.Any(),
                        HasAllergies = patient.Allergies.Any(),
                        HasLifestyle = patient.Lifestyle != null,
                        HasVisitIntake = patient.VisitIntakes.Any(),
                        HasConsent = patient.Consent != null,
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

        [HttpGet("insurance")]
        public async Task<ActionResult<List<InsurancePolicyDto>>> GetInsurance()
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
                    return Ok(new List<InsurancePolicyDto>()); // Return empty list if no patient record
                }

                var insurancePolicies = await _context.InsurancePolicies
                    .Where(ip => ip.PatientId == patient.Id)
                    .ToListAsync();

                var insuranceDtos = insurancePolicies.Select(ip => new InsurancePolicyDto
                {
                    Id = ip.Id,
                    Provider = ip.Provider ?? "",
                    PolicyNumber = ip.PolicyNumber ?? "",
                    GroupNumber = ip.GroupNumber ?? "",
                    PolicyholderName = ip.PolicyholderName ?? "",
                    RelationshipToPatient = ip.RelationshipToPatient ?? ""
                }).ToList();

                return Ok(insuranceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving insurance policies");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-insurance")]
        public async Task<ActionResult<SaveInsuranceResponse>> SaveInsurance([FromBody] SaveInsuranceRequest request)
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

                if (request.Insurance.Id > 0)
                {
                    // Update existing insurance policy
                    var existingInsurance = await _context.InsurancePolicies
                        .FirstOrDefaultAsync(ip => ip.Id == request.Insurance.Id && ip.PatientId == patient.Id);

                    if (existingInsurance == null)
                    {
                        return NotFound("Insurance policy not found or access denied");
                    }

                    existingInsurance.Provider = request.Insurance.Provider;
                    existingInsurance.PolicyNumber = request.Insurance.PolicyNumber;
                    existingInsurance.GroupNumber = request.Insurance.GroupNumber;
                    existingInsurance.PolicyholderName = request.Insurance.PolicyholderName;
                    existingInsurance.RelationshipToPatient = request.Insurance.RelationshipToPatient;
                }
                else
                {
                    // Create new insurance policy
                    var newInsurance = new InsurancePolicy
                    {
                        PatientId = patient.Id,
                        Provider = request.Insurance.Provider,
                        PolicyNumber = request.Insurance.PolicyNumber,
                        GroupNumber = request.Insurance.GroupNumber,
                        PolicyholderName = request.Insurance.PolicyholderName,
                        RelationshipToPatient = request.Insurance.RelationshipToPatient
                    };

                    _context.InsurancePolicies.Add(newInsurance);
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveInsuranceResponse
                {
                    Success = true,
                    Message = "Insurance policy saved successfully!",
                    InsuranceId = request.Insurance.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving insurance policy");
                return StatusCode(500, new SaveInsuranceResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpDelete("insurance/{insuranceId}")]
        public async Task<ActionResult<DeleteInsuranceResponse>> DeleteInsurance(int insuranceId)
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

                var insurance = await _context.InsurancePolicies
                    .FirstOrDefaultAsync(ip => ip.Id == insuranceId && ip.PatientId == patient.Id);

                if (insurance == null)
                {
                    return NotFound("Insurance policy not found or access denied");
                }

                _context.InsurancePolicies.Remove(insurance);
                await _context.SaveChangesAsync();

                return Ok(new DeleteInsuranceResponse
                {
                    Success = true,
                    Message = "Insurance policy deleted successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting insurance policy");
                return StatusCode(500, new DeleteInsuranceResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("emergency-contacts")]
        public async Task<ActionResult<List<EmergencyContactDto>>> GetEmergencyContacts()
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
                    return Ok(new List<EmergencyContactDto>()); // Return empty list if no patient record
                }

                var emergencyContacts = await _context.EmergencyContacts
                    .Where(ec => ec.PatientId == patient.Id)
                    .ToListAsync();

                var emergencyContactDtos = emergencyContacts.Select(ec => new EmergencyContactDto
                {
                    Id = ec.Id,
                    FirstName = ec.FirstName ?? "",
                    LastName = ec.LastName ?? "",
                    Phone = ec.Phone ?? "",
                    Relationship = ec.Relationship ?? ""
                }).ToList();

                return Ok(emergencyContactDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emergency contacts");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-emergency-contact")]
        public async Task<ActionResult<SaveEmergencyContactResponse>> SaveEmergencyContact([FromBody] SaveEmergencyContactRequest request)
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

                if (request.EmergencyContact.Id > 0)
                {
                    // Update existing emergency contact
                    var existingContact = await _context.EmergencyContacts
                        .FirstOrDefaultAsync(ec => ec.Id == request.EmergencyContact.Id && ec.PatientId == patient.Id);

                    if (existingContact == null)
                    {
                        return NotFound("Emergency contact not found or access denied");
                    }

                    existingContact.FirstName = request.EmergencyContact.FirstName;
                    existingContact.LastName = request.EmergencyContact.LastName;
                    existingContact.Phone = request.EmergencyContact.Phone;
                    existingContact.Relationship = request.EmergencyContact.Relationship;
                }
                else
                {
                    // Create new emergency contact
                    var newContact = new EmergencyContact
                    {
                        PatientId = patient.Id,
                        FirstName = request.EmergencyContact.FirstName,
                        LastName = request.EmergencyContact.LastName,
                        Phone = request.EmergencyContact.Phone,
                        Relationship = request.EmergencyContact.Relationship
                    };

                    _context.EmergencyContacts.Add(newContact);
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveEmergencyContactResponse
                {
                    Success = true,
                    Message = "Emergency contact saved successfully!",
                    EmergencyContactId = request.EmergencyContact.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving emergency contact");
                return StatusCode(500, new SaveEmergencyContactResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpDelete("emergency-contact/{contactId}")]
        public async Task<ActionResult<DeleteEmergencyContactResponse>> DeleteEmergencyContact(int contactId)
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

                var contact = await _context.EmergencyContacts
                    .FirstOrDefaultAsync(ec => ec.Id == contactId && ec.PatientId == patient.Id);

                if (contact == null)
                {
                    return NotFound("Emergency contact not found or access denied");
                }

                _context.EmergencyContacts.Remove(contact);
                await _context.SaveChangesAsync();

                return Ok(new DeleteEmergencyContactResponse
                {
                    Success = true,
                    Message = "Emergency contact deleted successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting emergency contact");
                return StatusCode(500, new DeleteEmergencyContactResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("conditions")]
        public async Task<ActionResult<List<PatientConditionDto>>> GetConditions()
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
                    return Ok(new List<PatientConditionDto>()); // Return empty list if no patient record
                }

                var patientConditions = await _context.PatientConditions
                    .Include(pc => pc.Condition)
                    .Where(pc => pc.PatientId == patient.Id)
                    .ToListAsync();

                var conditionDtos = patientConditions.Select(pc => new PatientConditionDto
                {
                    Id = pc.ConditionId,
                    ConditionName = pc.Condition.Name ?? ""
                }).ToList();

                return Ok(conditionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conditions");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-condition")]
        public async Task<ActionResult<SaveConditionResponse>> SaveCondition([FromBody] SaveConditionRequest request)
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

                // Check if this condition already exists for the patient
                var existingPatientCondition = await _context.PatientConditions
                    .Include(pc => pc.Condition)
                    .FirstOrDefaultAsync(pc => pc.PatientId == patient.Id && pc.ConditionId == request.Condition.Id);

                if (existingPatientCondition != null)
                {
                    return BadRequest("This condition is already added to your medical history.");
                }

                // Check if the condition exists in the system, if not create it
                var condition = await _context.Conditions
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Condition.ConditionName.ToLower());

                if (condition == null)
                {
                    // Create new condition
                    condition = new Condition
                    {
                        Name = request.Condition.ConditionName,
                        IsActive = true // Default to true, but this isn't used in the UI anymore
                    };
                    _context.Conditions.Add(condition);
                    await _context.SaveChangesAsync();
                }

                // Create patient-condition relationship
                var patientCondition = new PatientCondition
                {
                    PatientId = patient.Id,
                    ConditionId = condition.Id
                };

                _context.PatientConditions.Add(patientCondition);
                await _context.SaveChangesAsync();

                return Ok(new SaveConditionResponse
                {
                    Success = true,
                    Message = "Condition saved successfully!",
                    ConditionId = condition.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving condition");
                return StatusCode(500, new SaveConditionResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpDelete("condition/{conditionId}")]
        public async Task<ActionResult<DeleteConditionResponse>> DeleteCondition(int conditionId)
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

                var patientCondition = await _context.PatientConditions
                    .FirstOrDefaultAsync(pc => pc.PatientId == patient.Id && pc.ConditionId == conditionId);

                if (patientCondition == null)
                {
                    return NotFound("Condition not found or access denied");
                }

                _context.PatientConditions.Remove(patientCondition);
                await _context.SaveChangesAsync();

                return Ok(new DeleteConditionResponse
                {
                    Success = true,
                    Message = "Condition removed successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting condition");
                return StatusCode(500, new DeleteConditionResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("procedures")]
        public async Task<ActionResult<List<ProcedureDto>>> GetProcedures()
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
                    return Ok(new List<ProcedureDto>()); // Return empty list if no patient record
                }

                var procedures = await _context.Procedures
                    .Where(p => p.PatientId == patient.Id)
                    .ToListAsync();

                var procedureDtos = procedures.Select(p => new ProcedureDto
                {
                    Id = p.Id,
                    ProcedureName = p.ProcedureName ?? "",
                    ProcedureDate = p.ProcedureDate,
                    Notes = p.Notes ?? ""
                }).ToList();

                return Ok(procedureDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving procedures");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-procedure")]
        public async Task<ActionResult<SaveProcedureResponse>> SaveProcedure([FromBody] SaveProcedureRequest request)
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

                if (request.Procedure.Id > 0)
                {
                    // Update existing procedure
                    var existingProcedure = await _context.Procedures
                        .FirstOrDefaultAsync(p => p.Id == request.Procedure.Id && p.PatientId == patient.Id);

                    if (existingProcedure == null)
                    {
                        return NotFound("Procedure not found or access denied");
                    }

                    existingProcedure.ProcedureName = request.Procedure.ProcedureName;
                    existingProcedure.ProcedureDate = request.Procedure.ProcedureDate;
                    existingProcedure.Notes = request.Procedure.Notes;
                }
                else
                {
                    // Create new procedure
                    var newProcedure = new Procedure
                    {
                        PatientId = patient.Id,
                        ProcedureName = request.Procedure.ProcedureName,
                        ProcedureDate = request.Procedure.ProcedureDate,
                        Notes = request.Procedure.Notes
                    };

                    _context.Procedures.Add(newProcedure);
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveProcedureResponse
                {
                    Success = true,
                    Message = "Procedure saved successfully!",
                    ProcedureId = request.Procedure.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving procedure");
                return StatusCode(500, new SaveProcedureResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpDelete("procedure/{procedureId}")]
        public async Task<ActionResult<DeleteProcedureResponse>> DeleteProcedure(int procedureId)
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

                var procedure = await _context.Procedures
                    .FirstOrDefaultAsync(p => p.Id == procedureId && p.PatientId == patient.Id);

                if (procedure == null)
                {
                    return NotFound("Procedure not found or access denied");
                }

                _context.Procedures.Remove(procedure);
                await _context.SaveChangesAsync();

                return Ok(new DeleteProcedureResponse
                {
                    Success = true,
                    Message = "Procedure deleted successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting procedure");
                return StatusCode(500, new DeleteProcedureResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("visit-intake")]
        public async Task<ActionResult<GetVisitIntakeResponse>> GetVisitIntake()
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
                    return Ok(new GetVisitIntakeResponse { VisitIntake = null });
                }

                var visitIntake = await _context.VisitIntakes
                    .FirstOrDefaultAsync(vi => vi.PatientId == patient.Id);

                var response = new GetVisitIntakeResponse();

                if (visitIntake != null)
                {
                    response.VisitIntake = new VisitIntakeDto
                    {
                        Id = visitIntake.Id,
                        PrimaryReason = visitIntake.PrimaryReason ?? "",
                        TreatedBefore = visitIntake.TreatedBefore
                    };
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving visit intake");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-visit-intake")]
        public async Task<ActionResult<SaveVisitIntakeResponse>> SaveVisitIntake([FromBody] SaveVisitIntakeRequest request)
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

                var existingVisitIntake = await _context.VisitIntakes
                    .FirstOrDefaultAsync(vi => vi.PatientId == patient.Id);

                if (existingVisitIntake != null)
                {
                    // Update existing visit intake
                    existingVisitIntake.PrimaryReason = request.VisitIntake.PrimaryReason;
                    existingVisitIntake.TreatedBefore = request.VisitIntake.TreatedBefore;
                }
                else
                {
                    // Create new visit intake
                    var newVisitIntake = new VisitIntake
                    {
                        PatientId = patient.Id,
                        PrimaryReason = request.VisitIntake.PrimaryReason,
                        TreatedBefore = request.VisitIntake.TreatedBefore
                    };

                    _context.VisitIntakes.Add(newVisitIntake);
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveVisitIntakeResponse
                {
                    Success = true,
                    Message = "Visit information saved successfully!",
                    VisitIntakeId = existingVisitIntake?.Id ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving visit intake");
                return StatusCode(500, new SaveVisitIntakeResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet("lifestyle")]
        public async Task<ActionResult<GetLifestyleResponse>> GetLifestyle()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .Include(p => p.Lifestyle)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                _logger.LogInformation("Patient found: {PatientFound}, Lifestyle found: {LifestyleFound}", 
                    patient != null, patient?.Lifestyle != null);

                if (patient == null)
                {
                    return Ok(new GetLifestyleResponse { Lifestyle = null });
                }

                var response = new GetLifestyleResponse();

                if (patient.Lifestyle != null)
                {
                    _logger.LogInformation("Database lifestyle values - Tobacco: {Tobacco}, Alcohol: {Alcohol}, Drugs: {Drugs}",
                        patient.Lifestyle.TobaccoUse, patient.Lifestyle.AlcoholUse, patient.Lifestyle.RecreationalDrugs);

                    response.Lifestyle = new LifestyleDto
                    {
                        PatientId = patient.Lifestyle.PatientId,
                        TobaccoUse = (July2025Capstone.Shared.Models.TobaccoUse)patient.Lifestyle.TobaccoUse,
                        AlcoholUse = (July2025Capstone.Shared.Models.AlcoholUse)patient.Lifestyle.AlcoholUse,
                        RecreationalDrugs = patient.Lifestyle.RecreationalDrugs
                    };

                    _logger.LogInformation("Mapped DTO values - Tobacco: {Tobacco} ({TobaccoInt}), Alcohol: {Alcohol} ({AlcoholInt}), Drugs: {Drugs}",
                        response.Lifestyle.TobaccoUse, (int)response.Lifestyle.TobaccoUse, 
                        response.Lifestyle.AlcoholUse, (int)response.Lifestyle.AlcoholUse, 
                        response.Lifestyle.RecreationalDrugs);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lifestyle information");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-lifestyle")]
        public async Task<ActionResult<SaveLifestyleResponse>> SaveLifestyle([FromBody] SaveLifestyleRequest request)
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
                    .Include(p => p.Lifestyle)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return BadRequest("Patient record not found. Please complete personal information first.");
                }

                if (patient.Lifestyle != null)
                {
                    // Update existing lifestyle
                    patient.Lifestyle.TobaccoUse = (int)request.Lifestyle.TobaccoUse;
                    patient.Lifestyle.AlcoholUse = (int)request.Lifestyle.AlcoholUse;
                    patient.Lifestyle.RecreationalDrugs = request.Lifestyle.RecreationalDrugs;
                }
                else
                {
                    // Create new lifestyle record
                    var newLifestyle = new Lifestyle
                    {
                        PatientId = patient.Id,
                        TobaccoUse = (int)request.Lifestyle.TobaccoUse,
                        AlcoholUse = (int)request.Lifestyle.AlcoholUse,
                        RecreationalDrugs = request.Lifestyle.RecreationalDrugs
                    };

                    _context.Lifestyles.Add(newLifestyle);
                    patient.Lifestyle = newLifestyle;
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveLifestyleResponse
                {
                    Success = true,
                    Message = "Lifestyle information saved successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving lifestyle information");
                return StatusCode(500, new SaveLifestyleResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpDelete("lifestyle/{patientId}")]
        public async Task<ActionResult> DeleteLifestyle(int patientId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .Include(p => p.Lifestyle)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Id == patientId);

                if (patient == null)
                {
                    return NotFound("Patient not found");
                }

                if (patient.Lifestyle != null)
                {
                    _context.Lifestyles.Remove(patient.Lifestyle);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Success = true, Message = "Lifestyle data cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing lifestyle data");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("consent")]
        public async Task<ActionResult<GetConsentResponse>> GetConsent()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .Include(p => p.Consent)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return Ok(new GetConsentResponse { Consent = null });
                }

                var response = new GetConsentResponse();

                if (patient.Consent != null)
                {
                    response.Consent = new ConsentDto
                    {
                        Id = patient.Consent.Id,
                        PatientId = patient.Consent.PatientId,
                        HasConsented = true, // If consent exists, they have consented
                        SignedAt = patient.Consent.SignedAt,
                        SignatureName = patient.Consent.SignatureName,
                        PatientName = $"{patient.FirstName} {patient.LastName}"
                    };
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consent information");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("save-consent")]
        public async Task<ActionResult<SaveConsentResponse>> SaveConsent([FromBody] SaveConsentRequest request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .Include(p => p.Consent)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return BadRequest("Patient record not found. Please complete personal information first.");
                }

                if (patient.Consent != null)
                {
                    // Update existing consent
                    patient.Consent.SignedAt = DateTime.UtcNow;
                    patient.Consent.SignatureName = request.Consent.SignatureName;
                }
                else
                {
                    // Create new consent record
                    var newConsent = new Consent
                    {
                        PatientId = patient.Id,
                        SignedAt = DateTime.UtcNow,
                        SignatureName = request.Consent.SignatureName
                    };

                    _context.Consents.Add(newConsent);
                    patient.Consent = newConsent;
                }

                await _context.SaveChangesAsync();

                return Ok(new SaveConsentResponse
                {
                    Success = true,
                    Message = "Consent saved successfully!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving consent");
                return StatusCode(500, new SaveConsentResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpPost("complete-checkin")]
        public async Task<ActionResult<CompleteCheckInResponse>> CompleteCheckIn()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Unable to determine user identity");
                }

                var patient = await _context.Patients
                    .Include(p => p.Consent)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    return BadRequest("Patient record not found.");
                }

                if (patient.Consent == null)
                {
                    return BadRequest("Consent is required to complete check-in.");
                }

                // Mark check-in as complete (you could add a flag to patient if needed)
                // For now, we'll just return success since consent exists

                return Ok(new CompleteCheckInResponse
                {
                    Success = true,
                    Message = "Check-in completed successfully! Thank you.",
                    PatientId = patient.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing check-in");
                return StatusCode(500, new CompleteCheckInResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
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
        public bool HasVisitIntake { get; set; }
        public bool HasConsent { get; set; }
        public bool OverallComplete { get; set; }
    }

    public class GetVisitIntakeResponse
    {
        public VisitIntakeDto? VisitIntake { get; set; }
    }

    public class SaveVisitIntakeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int VisitIntakeId { get; set; }
    }

    public class SaveVisitIntakeRequest
    {
        public VisitIntakeDto VisitIntake { get; set; } = new();
    }

    public class GetLifestyleResponse
    {
        public LifestyleDto? Lifestyle { get; set; }
    }

    public class SaveLifestyleRequest
    {
        public LifestyleDto Lifestyle { get; set; } = new();
    }

    public class SaveLifestyleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GetConsentResponse
    {
        public ConsentDto? Consent { get; set; }
    }

    public class SaveConsentRequest
    {
        public ConsentDto Consent { get; set; } = new();
    }

    public class SaveConsentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CompleteCheckInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PatientId { get; set; }
    }

    public class SaveConditionRequest
    {
        public PatientConditionDto Condition { get; set; } = new();
    }

    public class SaveConditionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ConditionId { get; set; }
    }

    public class DeleteConditionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SaveProcedureRequest
    {
        public ProcedureDto Procedure { get; set; } = new();
    }

    public class SaveProcedureResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcedureId { get; set; }
    }
    }
}