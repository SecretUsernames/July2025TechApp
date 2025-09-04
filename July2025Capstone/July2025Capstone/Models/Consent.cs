using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Consent
    {
        [Key]
        public int Id { get; set; }
        
        public int PatientId { get; set; }
        
        public DateTime SignedAt { get; set; }
        
        [StringLength(100)]
        public string? SignatureName { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}