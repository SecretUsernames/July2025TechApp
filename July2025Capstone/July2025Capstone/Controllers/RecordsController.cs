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
    public class RecordsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RecordsController> _logger;

        public RecordsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<RecordsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<RecordsUploadResponse>> UploadMedicalRecords([FromForm] RecordsUploadRequest request)
        {
            try
            {
                // For testing, bypass user authentication
                // var userId = _userManager.GetUserId(User);
                // if (string.IsNullOrEmpty(userId))
                // {
                //     return Unauthorized();
                // }

                var response = new RecordsUploadResponse
                {
                    Success = true,
                    Message = "Medical records uploaded and analyzed successfully!",
                    Results = new List<AnalysisResult>()
                };

                if (request.Files == null || !request.Files.Any())
                {
                    return BadRequest(new RecordsUploadResponse
                    {
                        Success = false,
                        Message = "No files were uploaded."
                    });
                }

                foreach (var file in request.Files)
                {
                    _logger.LogInformation($"Processing file: {file.FileName}, Size: {file.Length} bytes, Type: {file.ContentType}");

                    var analysisResult = new AnalysisResult
                    {
                        FileName = file.FileName,
                        ProcessedDate = DateTime.UtcNow
                    };

                    try
                    {
                        // Simulate file processing based on file type
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();

                        switch (fileExtension)
                        {
                            case ".pdf":
                                analysisResult = await ProcessPdfFile(file);
                                break;
                            case ".xml":
                                analysisResult = await ProcessXmlFile(file);
                                break;
                            case ".json":
                                analysisResult = await ProcessJsonFile(file);
                                break;
                            case ".csv":
                                analysisResult = await ProcessCsvFile(file);
                                break;
                            default:
                                analysisResult.Status = "Warning";
                                analysisResult.ExtractedData = "Unsupported file type";
                                analysisResult.Summary = $"File type {fileExtension} is not currently supported for automatic analysis.";
                                break;
                        }

                        // Here you would typically:
                        // 1. Save file to storage (Azure Blob, local storage, etc.)
                        // 2. Extract and parse medical data
                        // 3. Run analysis/ML models
                        // 4. Save results to database
                        // 5. Generate insights and alerts

                        // TODO: Save to database
                        // var medicalRecord = new MedicalRecord
                        // {
                        //     UserId = userId,
                        //     FileName = file.FileName,
                        //     ContentType = file.ContentType,
                        //     FileSize = file.Length,
                        //     ExtractedData = analysisResult.ExtractedData,
                        //     UploadDate = DateTime.UtcNow
                        // };
                        // _context.MedicalRecords.Add(medicalRecord);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing file {file.FileName}");
                        analysisResult.Status = "Error";
                        analysisResult.ExtractedData = $"Error processing file: {ex.Message}";
                        analysisResult.Summary = "File could not be processed due to an error.";
                    }

                    response.Results.Add(analysisResult);
                }

                // TODO: Save changes to database
                // await _context.SaveChangesAsync();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading medical records");
                return StatusCode(500, new RecordsUploadResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        private async Task<AnalysisResult> ProcessPdfFile(IFormFile file)
        {
            // Mock PDF processing
            await Task.Delay(1000); // Simulate processing time

            return new AnalysisResult
            {
                FileName = file.FileName,
                Status = "Success",
                ExtractedData = JsonSerializer.Serialize(new
                {
                    DocumentType = "Lab Report",
                    PatientName = "John Doe",
                    TestDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
                    Results = new[]
                    {
                        new { Test = "Complete Blood Count", Result = "Normal", Units = "cells/?L" },
                        new { Test = "Glucose", Result = "95", Units = "mg/dL" },
                        new { Test = "Cholesterol", Result = "180", Units = "mg/dL" }
                    }
                }, new JsonSerializerOptions { WriteIndented = true }),
                Summary = "Lab report processed successfully. All values within normal ranges.",
                ProcessedDate = DateTime.UtcNow
            };
        }

        private async Task<AnalysisResult> ProcessXmlFile(IFormFile file)
        {
            // Mock XML/HL7 processing
            await Task.Delay(800);

            return new AnalysisResult
            {
                FileName = file.FileName,
                Status = "Success",
                ExtractedData = JsonSerializer.Serialize(new
                {
                    DocumentType = "Clinical Document (CCD)",
                    PatientID = "12345",
                    Medications = new[] { "Lisinopril 10mg", "Metformin 500mg" },
                    Allergies = new[] { "Penicillin", "Shellfish" },
                    VitalSigns = new { BP = "120/80", HR = "72", Temp = "98.6°F" }
                }, new JsonSerializerOptions { WriteIndented = true }),
                Summary = "Clinical document processed. Found 2 medications and 2 allergies on record.",
                ProcessedDate = DateTime.UtcNow
            };
        }

        private async Task<AnalysisResult> ProcessJsonFile(IFormFile file)
        {
            // Mock JSON processing
            await Task.Delay(500);

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            try
            {
                var jsonDoc = JsonDocument.Parse(content);
                return new AnalysisResult
                {
                    FileName = file.FileName,
                    Status = "Success",
                    ExtractedData = content,
                    Summary = "JSON file validated and processed successfully.",
                    ProcessedDate = DateTime.UtcNow
                };
            }
            catch (JsonException)
            {
                return new AnalysisResult
                {
                    FileName = file.FileName,
                    Status = "Error",
                    ExtractedData = "Invalid JSON format",
                    Summary = "File contains invalid JSON and could not be parsed.",
                    ProcessedDate = DateTime.UtcNow
                };
            }
        }

        private async Task<AnalysisResult> ProcessCsvFile(IFormFile file)
        {
            // Mock CSV processing
            await Task.Delay(600);

            return new AnalysisResult
            {
                FileName = file.FileName,
                Status = "Success",
                ExtractedData = JsonSerializer.Serialize(new
                {
                    DocumentType = "CSV Data",
                    RowCount = 156,
                    Columns = new[] { "Date", "Test", "Result", "Reference Range" },
                    SampleData = new[]
                    {
                        new { Date = "2024-01-15", Test = "Hemoglobin", Result = "14.2", ReferenceRange = "12.0-16.0" },
                        new { Date = "2024-01-15", Test = "Hematocrit", Result = "42.1", ReferenceRange = "36.0-48.0" }
                    }
                }, new JsonSerializerOptions { WriteIndented = true }),
                Summary = "CSV file processed. Found 156 test results across multiple dates.",
                ProcessedDate = DateTime.UtcNow
            };
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<RecordSummary>>> GetUploadHistory()
        {
            try
            {
                // For testing, return mock data
                var records = new List<RecordSummary>
                {
                    new RecordSummary
                    {
                        Id = "1",
                        FileName = "lab_results_2024.pdf",
                        UploadDate = DateTime.Now.AddDays(-5),
                        FileType = "PDF",
                        Status = "Processed"
                    },
                    new RecordSummary
                    {
                        Id = "2",
                        FileName = "discharge_summary.xml",
                        UploadDate = DateTime.Now.AddDays(-12),
                        FileType = "XML",
                        Status = "Processed"
                    }
                };

                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upload history");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    // Model classes for API requests/responses
    public class RecordsUploadRequest
    {
        public List<IFormFile> Files { get; set; } = new();
    }

    public class RecordsUploadResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<AnalysisResult> Results { get; set; } = new();
    }

    public class AnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ExtractedData { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime ProcessedDate { get; set; }
    }

    public class RecordSummary
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}