using System.ComponentModel.DataAnnotations;
using July2025Capstone.Data;

namespace July2025Capstone.Models
{
    public class VitalGlucose
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        public decimal GlucoseValue { get; set; }
        
        public DateTime DateMeasured { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
    }
}