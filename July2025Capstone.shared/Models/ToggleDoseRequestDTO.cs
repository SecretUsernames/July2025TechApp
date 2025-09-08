using July2025Capstone.Client.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace July2025Capstone.Shared.Models
{
    public class ToggleDoseRequest
    {
        public int MedicationId { get; set; }
        public int DayOfWeek { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
    }

}
