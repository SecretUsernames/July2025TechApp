using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class VisitIntake
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        public string PrimaryReason { get; set; }
        
        public bool TreatedBefore { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}