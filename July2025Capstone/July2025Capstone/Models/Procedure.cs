using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace July2025Capstone.Models
{
    public class Procedure
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        public string ProcedureName { get; set; }
        
        public DateOnly ProcedureDate { get; set; }
        
        public string Notes { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}