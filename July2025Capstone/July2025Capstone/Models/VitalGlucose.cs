using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class VitalGlucose
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public decimal GlucoseValue { get; set; }
        
        public DateTime DateMeasured { get; set; }
    }
}