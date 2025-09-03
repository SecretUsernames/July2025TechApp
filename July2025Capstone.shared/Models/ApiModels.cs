namespace July2025Capstone.Shared.Models
{
    // API Request/Response Models
    public class SavePersonalInfoRequest
    {
        public PersonalInfoDto PersonalInfo { get; set; } = new();
        public AddressDto? Address { get; set; }
    }

    public class SavePersonalInfoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int PatientId { get; set; }
    }

    public class GetPersonalInfoResponse
    {
        public PersonalInfoDto PersonalInfo { get; set; } = new();
        public AddressDto? Address { get; set; }
    }

    public class SaveMedicationRequest
    {
        public MedicationDto Medication { get; set; } = new();
    }

    public class SaveMedicationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int MedicationId { get; set; }
    }

    public class DeleteMedicationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class SaveAllergyRequest
    {
        public AllergyDto Allergy { get; set; } = new();
    }

    public class SaveAllergyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int AllergyId { get; set; }
    }

    public class DeleteAllergyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class SaveInsuranceRequest
    {
        public InsurancePolicyDto Insurance { get; set; } = new();
    }

    public class SaveInsuranceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int InsuranceId { get; set; }
    }

    public class DeleteInsuranceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class SaveEmergencyContactRequest
    {
        public EmergencyContactDto EmergencyContact { get; set; } = new();
    }

    public class SaveEmergencyContactResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int EmergencyContactId { get; set; }
    }

    public class DeleteEmergencyContactResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class CheckinFormSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsComplete { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
    }
}