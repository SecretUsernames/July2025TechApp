using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Pharmacy
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public string Phone { get; set; }
        
        [Required]
        public int AddressId { get; set; }

        // Navigation properties
        public virtual Address Address { get; set; } = null!;
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}