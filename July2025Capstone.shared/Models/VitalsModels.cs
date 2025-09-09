using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Shared.Models
{
    public class VitalBloodPressure
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        [Range(60, 300)]
        public int Systolic { get; set; }
        [Required]
        [Range(40, 200)]
        public int Diastolic { get; set; }
        public DateTime DateMeasured { get; set; }
    }

    public class VitalGlucose
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        [Range(20, 600)]
        public decimal GlucoseValue { get; set; }
        public DateTime DateMeasured { get; set; }
    }

    public class VitalWeight
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        [Range(1, 1000)]
        public decimal WeightValue { get; set; }
        [Required]
        public WeightUnit Unit { get; set; }
        public DateTime DateMeasured { get; set; }
    }

    public enum WeightUnit
    {
        [Display(Name = "lbs")]
        Pounds = 0,
        
        [Display(Name = "kg")]
        Kilograms = 1
    }
}
