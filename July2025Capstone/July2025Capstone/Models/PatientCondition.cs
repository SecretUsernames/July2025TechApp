using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class PatientCondition
    {
        // Cross-join table between Patient and Condition

        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public int ConditionId { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Condition Condition { get; set; } = null!;
    }
}