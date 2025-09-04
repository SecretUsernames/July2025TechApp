using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using July2025Capstone.Data;

namespace July2025Capstone.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        public int? AddressId { get; set; } // Changed from int to int? (nullable)
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        public DateOnly DateOfBirth { get; set; }
        
        public int Gender { get; set; }
        
        public string Email { get; set; }
        
        public string Phone { get; set; }
        
        public int PreferredContactMethod { get; set; } // enum: Phone=0, Email=1, Text=2

        // Navigation properties
        public virtual Address? Address { get; set; }
        public virtual Pharmacy? PreferredPharmacy { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
        
        // Collections
        public virtual ICollection<Medication> Medications { get; set; } = new List<Medication>();
        public virtual ICollection<Allergy> Allergies { get; set; } = new List<Allergy>();
        public virtual ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
        public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
        public virtual ICollection<InsurancePolicy> InsurancePolicies { get; set; } = new List<InsurancePolicy>();
        public virtual ICollection<Procedure> Procedures { get; set; } = new List<Procedure>();
        public virtual ICollection<VisitIntake> VisitIntakes { get; set; } = new List<VisitIntake>();
        
        // One-to-one relationships
        public virtual Lifestyle? Lifestyle { get; set; }
        public virtual Consent? Consent { get; set; }
    }
}