using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Lifestyle
    {
        [Key]
        public int PatientId { get; set; }
        
        public int TobaccoUse { get; set; } // enum: Yes/No/Former
        
        public int AlcoholUse { get; set; } // enum: Yes/No/Occasionally
        
        public bool RecreationalDrugs { get; set; } // yes/no

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}