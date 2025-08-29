namespace July2025Capstone.Models
{
    // DTOs for API responses and client communication
    // These are NOT database entities - keep them separate from EF models
    
    public class VitalReading
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int Systolic { get; set; }
        public int Diastolic { get; set; }
    }

    public class Alert
    {
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class UploadItem
    {
        public string Name { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
    }

    public class RecentActivity
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}