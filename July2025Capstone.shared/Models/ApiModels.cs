namespace July2025Capstone.Shared.Models
{
    // API Request/Response Models
    public class SavePersonalInfoRequest
    {
        public PersonalInfoDto PersonalInfo { get; set; } = new();
    }

    public class SavePersonalInfoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int PatientId { get; set; }
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