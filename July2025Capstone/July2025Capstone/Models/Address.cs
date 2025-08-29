using System.ComponentModel.DataAnnotations;

namespace July2025Capstone.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }
        
        public string Street { get; set; }
        
        public string City { get; set; }
        
        public string State { get; set; }
        
        public string PostalCode { get; set; }
        
        public string Country { get; set; }

        // Navigation properties
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public virtual ICollection<Pharmacy> Pharmacies { get; set; } = new List<Pharmacy>();
    }
}