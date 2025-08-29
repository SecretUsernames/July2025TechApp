using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Allergy
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        public string Allergen { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}