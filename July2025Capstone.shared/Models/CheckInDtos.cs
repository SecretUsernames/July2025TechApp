using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Shared.Models
{
    // Enums that match your database model
    public enum GenderType
    {
        Male = 0,
        Female = 1,
        Other = 2
    }

    public enum ContactMethod
    {
        Phone = 0,
        Email = 1,
        Text = 2
    }

    public enum TobaccoUse
    {
        Never = 0,
        Former = 1,
        Current = 2
    }

    public enum AlcoholUse
    {
        Never = 0,
        Occasional = 1,
        Regular = 2,
        Heavy = 3
    }

    // Personal Information DTO
    public class PersonalInfoDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        public string FirstName { get; set; } = "";
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        public string LastName { get; set; } = "";
        
        [Required(ErrorMessage = "Date of birth is required")]
        public DateOnly DateOfBirth { get; set; }
        
        public GenderType Gender { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "";
        
        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot be longer than 20 characters")]
        public string Phone { get; set; } = "";

        public ContactMethod PreferredContactMethod { get; set; }
    }

    // Address DTO
    public class AddressDto
    {
        [Required(ErrorMessage = "Street address is required")]
        [StringLength(200)]
        public string Street { get; set; } = "";
        
        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = "";
        
        [Required(ErrorMessage = "State is required")]
        [StringLength(50)]
        public string State { get; set; } = "";
        
        [Required(ErrorMessage = "Postal code is required")]
        [StringLength(20)]
        public string PostalCode { get; set; } = "";
        
        [Required(ErrorMessage = "Country is required")]
        [StringLength(50)]
        public string Country { get; set; } = "United States";
    }

    // Insurance DTO
    public class InsurancePolicyDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Insurance provider is required")]
        [StringLength(100)]
        public string Provider { get; set; } = "";
        
        [Required(ErrorMessage = "Policy number is required")]
        [StringLength(50)]
        public string PolicyNumber { get; set; } = "";
        
        [Required(ErrorMessage = "Group number is required")]
        [StringLength(50)]
        public string GroupNumber { get; set; } = "";
        
        [Required(ErrorMessage = "Policyholder name is required")]
        [StringLength(100)]
        public string PolicyholderName { get; set; } = "";
        
        [Required(ErrorMessage = "Relationship to patient is required")]
        [StringLength(50)]
        public string RelationshipToPatient { get; set; } = "";
    }

    // Emergency Contact DTO
    public class EmergencyContactDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = "";
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = "";
        
        [Required(ErrorMessage = "Relationship is required")]
        [StringLength(50)]
        public string Relationship { get; set; } = "";
        
        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20)]
        public string Phone { get; set; } = "";
    }

    // Complete Patient Form DTO
    public class PatientFormDto
    {
        public int Id { get; set; }
        public PersonalInfoDto PersonalInfo { get; set; } = new();
        public AddressDto? Address { get; set; }
        public List<InsurancePolicyDto> InsurancePolicies { get; set; } = new();
        public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
    }
}