using System.ComponentModel.DataAnnotations;
using July2025Capstone.Data;

namespace July2025Capstone.Models
{
    public class VitalBloodPressure
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public int Systolic { get; set; }
        
        public int Diastolic { get; set; }
        
        public DateTime DateMeasured { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
    }
}