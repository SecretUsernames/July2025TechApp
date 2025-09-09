using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class VitalWeight
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
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