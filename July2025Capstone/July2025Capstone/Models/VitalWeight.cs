using System.ComponentModel.DataAnnotations;
using July2025Capstone.Data;

namespace July2025Capstone.Models
{
    public class VitalWeight
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        public decimal WeightValue { get; set; }
        
        [Required]
        public WeightUnit Unit { get; set; }
        
        public DateTime DateMeasured { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
    }

    public enum WeightUnit
    {
        [Display(Name = "lbs")]
        Pounds = 0,
        
        [Display(Name = "kg")]
        Kilograms = 1
    }
}