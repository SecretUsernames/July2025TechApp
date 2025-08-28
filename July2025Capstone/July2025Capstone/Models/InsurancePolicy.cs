using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class InsurancePolicy
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        public string Provider { get; set; }
        
        public string PolicyNumber { get; set; }
        
        public string GroupNumber { get; set; }
        
        public string PolicyholderName { get; set; }
        
        public string RelationshipToPatient { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}