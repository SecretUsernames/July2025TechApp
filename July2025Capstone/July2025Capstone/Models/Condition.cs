using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Condition
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
    }
}