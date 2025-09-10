using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Medication
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        public string Name { get; set; }
        
        public decimal DosageStrength { get; set; } // 100, 40, etc.
        
        public DosageUnit DosageUnit { get; set; } // mg, mcg, mL, g
        
        public string? CustomDosageUnit { get; set; } // for edge cases - now nullable

        public MedicationFrequency Frequency { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;

        public virtual ICollection<MedicationDose> Doses { get; set; } = new List<MedicationDose>();
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
}