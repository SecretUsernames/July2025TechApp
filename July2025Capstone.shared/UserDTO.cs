using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace July2025Capstone.Shared
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<MedicationDTO> ListOfMedications { get; set; }
        //TODO: add allergy list

        //TODO: add procedure list

        //TODO: add insurance policy

        //TODO: add emergency contact

        //TODO: add visit intake

        //TODO: add preferred pharmacy

        //TODO: add consent information
    }
}
