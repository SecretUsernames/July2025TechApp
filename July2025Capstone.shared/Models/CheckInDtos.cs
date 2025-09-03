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

    public enum DosageUnit
    {
        Milligrams,     // mg
        Micrograms,     // mcg
        Grams,          // g
        Milliliters,    // mL
        Liters,         // L
        Units,          // IU (International Units)
        Other           // for edge cases
    }

    public enum MedicationFrequency
    {
        OnceDaily,      // Once daily
        TwiceDaily,     // Twice daily
        ThreeDaily,     // Three times daily
        FourDaily,      // Four times daily
        AsNeeded        // As needed
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

    // Simple Allergy DTO (matches your existing model)
    public class AllergyDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Allergen name is required")]
        [StringLength(200, ErrorMessage = "Allergen name cannot be longer than 200 characters")]
        public string Allergen { get; set; } = "";
    }

    // Medication DTO
    public class MedicationDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Medication name is required")]
        [StringLength(200, ErrorMessage = "Medication name cannot be longer than 200 characters")]
        public string Name { get; set; } = "";
        
        [Required(ErrorMessage = "Dosage strength is required")]
        [Range(0.001, 99999, ErrorMessage = "Dosage strength must be greater than 0")]
        public decimal DosageStrength { get; set; }
        
        [Required(ErrorMessage = "Dosage unit is required")]
        public DosageUnit DosageUnit { get; set; }
        
        public string? CustomDosageUnit { get; set; }
        
        [Required(ErrorMessage = "Frequency is required")]
        public MedicationFrequency Frequency { get; set; }

        // Helper properties for display
        public string DosageDisplay => DosageUnit == DosageUnit.Other && !string.IsNullOrEmpty(CustomDosageUnit) 
            ? $"{DosageStrength} {CustomDosageUnit}" 
            : $"{DosageStrength} {GetDosageUnitString()}";

        public string FrequencyDisplay => GetFrequencyString();

        private string GetDosageUnitString()
        {
            return DosageUnit switch
            {
                DosageUnit.Milligrams => "mg",
                DosageUnit.Micrograms => "mcg",
                DosageUnit.Grams => "g",
                DosageUnit.Milliliters => "mL",
                DosageUnit.Liters => "L",
                DosageUnit.Units => "IU",
                DosageUnit.Other => CustomDosageUnit ?? "units",
                _ => "units"
            };
        }

        private string GetFrequencyString()
        {
            return Frequency switch
            {
                MedicationFrequency.OnceDaily => "Once daily",
                MedicationFrequency.TwiceDaily => "Twice daily",
                MedicationFrequency.ThreeDaily => "Three times daily",
                MedicationFrequency.FourDaily => "Four times daily",
                MedicationFrequency.AsNeeded => "As needed",
                _ => "Unknown"
            };
        }
    }

    // Complete Patient Form DTO
    public class PatientFormDto
    {
        public int Id { get; set; }
        public PersonalInfoDto PersonalInfo { get; set; } = new();
        public AddressDto? Address { get; set; }
        public List<InsurancePolicyDto> InsurancePolicies { get; set; } = new();
        public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
        public List<MedicationDto> Medications { get; set; } = new();
        public List<AllergyDto> Allergies { get; set; } = new();
    }
}