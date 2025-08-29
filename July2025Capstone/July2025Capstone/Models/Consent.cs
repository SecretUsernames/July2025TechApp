using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Consent
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        public DateTime SignedAt { get; set; }
        
        public byte[]? SignatureImage { get; set; } // optional

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}