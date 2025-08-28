using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class EmergencyContact
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        public string FirstName { get; set; }

        public string LastName { get; set; }
        
        public string Phone { get; set; }
        
        public string Relationship { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}