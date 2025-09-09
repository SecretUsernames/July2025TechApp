using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace July2025Capstone.Shared.Models
{
    public enum TimeOfDay
    {
        Morning = 0,
        Afternoon = 1,
        Evening = 2,
        Bedtime = 3
    }

    public class ToggleDoseRequest
    {
        public int MedicationId { get; set; }
        public int DayOfWeek { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
    }
}
