namespace July2025Capstone.Shared.Models
{
    public class VitalBloodPressure
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public int Systolic { get; set; }
        public int Diastolic { get; set; }
        public DateTime DateMeasured { get; set; }
    }

    public class VitalGlucose
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public decimal GlucoseValue { get; set; }
        public DateTime DateMeasured { get; set; }
    }

    public class VitalWeight
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public decimal WeightValue { get; set; }
        public string Unit { get; set; } = "lbs";
        public DateTime DateMeasured { get; set; }
    }
}
