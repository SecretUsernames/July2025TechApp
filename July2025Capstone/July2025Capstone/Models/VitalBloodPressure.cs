using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class VitalBloodPressure
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Systolic pressure is required")]
        [Range(60, 300, ErrorMessage = "Systolic pressure must be between 60 and 300")]
        public int Systolic { get; set; }
        
        [Required(ErrorMessage = "Diastolic pressure is required")]
        [Range(40, 200, ErrorMessage = "Diastolic pressure must be between 40 and 200")]
        public int Diastolic { get; set; }
        
        [Required(ErrorMessage = "Date measured is required")]
        public DateTime DateMeasured { get; set; }
    }
}