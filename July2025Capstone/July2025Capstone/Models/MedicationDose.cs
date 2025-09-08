using July2025Capstone.Client.Pages;
using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class MedicationDose
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MedicationId { get; set; } //Foreign Key to Medication table

        [Required]
        public int DayOfWeek { get; set; } // 0 = Sunday, 6 = Saturday

        [Required]
        public TimeOfDay TimeOfDay { get; set; } // Enum

        public bool Taken { get; set; } = false;

        public DateTime? TakenAt { get; set; }

        // Navigation property
        public virtual Medication Medication { get; set; } = null!;
    }
}
