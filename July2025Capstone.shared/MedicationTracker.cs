using July2025Capstone.Shared;
using July2025Capstone.Shared.Models;

namespace July2025Capstone.Client.Pages
{
    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Evening,
        Bedtime
    }

    public class MedicationDose
    {
        public int MedicationId { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
        public int DayOfWeek { get; set; } // 0 = Sunday, 6 = Saturday
        public bool Taken { get; set; }
        public DateTime? TakenAt { get; set; }
    }

    public class WeeklyStats
    {
        public int TotalScheduled { get; set; }
        public int TotalTaken { get; set; }
        public int TotalMissed { get; set; }
        public int AsNeededCount { get; set; }
    }

    public static class MedicationTracker
    {
        public static List<TimeOfDay> GetTimesForFrequency(MedicationFrequency frequency)
        {
            return frequency switch
            {
                MedicationFrequency.OnceDaily => new List<TimeOfDay> { TimeOfDay.Morning },
                MedicationFrequency.TwiceDaily => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Evening },
                MedicationFrequency.ThreeDaily => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening },
                MedicationFrequency.FourDaily => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening, TimeOfDay.Bedtime },
                MedicationFrequency.AsNeeded => new List<TimeOfDay> { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening, TimeOfDay.Bedtime },
                _ => new List<TimeOfDay>()
            };
        }

        public static string GetTimeDisplayName(TimeOfDay time)
        {
            return time switch
            {
                TimeOfDay.Morning => "Morning",
                TimeOfDay.Afternoon => "Afternoon",
                TimeOfDay.Evening => "Evening",
                TimeOfDay.Bedtime => "Bedtime",
                _ => ""
            };
        }

        public static string GetDayShortName(int dayIndex)
        {
            string[] days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            return dayIndex >= 0 && dayIndex < days.Length ? days[dayIndex] : "";
        }

        public static string GetDayName(int dayIndex)
        {
            string[] days = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            return dayIndex >= 0 && dayIndex < days.Length ? days[dayIndex] : "";
        }

        public static List<MedicationDose> InitializeDoses(List<MedicationDto> medications, List<MedicationDose> existingDoses = null)
        {
            var newDoses = new List<MedicationDose>();
            existingDoses ??= new List<MedicationDose>();

            foreach (var medication in medications)
            {
                var times = GetTimesForFrequency(medication.Frequency);

                for (int day = 0; day < 7; day++)
                {
                    foreach (var time in times)
                    {
                        var existingDose = existingDoses.FirstOrDefault(d =>
                            d.MedicationId == medication.Id &&
                            d.DayOfWeek == day &&
                            d.TimeOfDay == time);

                        if (existingDose != null)
                        {
                            newDoses.Add(existingDose);
                        }
                        else
                        {
                            newDoses.Add(new MedicationDose
                            {
                                MedicationId = medication.Id,
                                TimeOfDay = time,
                                DayOfWeek = day,
                                Taken = false
                            });
                        }
                    }
                }
            }

            return newDoses;
        }

        public static WeeklyStats CalculateWeeklyStats(List<MedicationDto> medications, List<MedicationDose> doses)
        {
            var scheduledMedications = medications.Where(m => m.Frequency != MedicationFrequency.AsNeeded).ToList();
            var asNeededMedications = medications.Where(m => m.Frequency == MedicationFrequency.AsNeeded).ToList();

            int totalScheduled = 0;
            int totalTaken = 0;
            int asNeededCount = 0;

            foreach (var medication in scheduledMedications)
            {
                var times = GetTimesForFrequency(medication.Frequency);
                var scheduledForMed = times.Count * 7; // 7 days
                var takenForMed = doses.Count(d => d.MedicationId == medication.Id && d.Taken);

                totalScheduled += scheduledForMed;
                totalTaken += takenForMed;
            }

            // Count as-needed medications taken
            foreach (var medication in asNeededMedications)
            {
                var takenCount = doses.Count(d => d.MedicationId == medication.Id && d.Taken);
                asNeededCount += takenCount;
            }

            return new WeeklyStats
            {
                TotalScheduled = totalScheduled,
                TotalTaken = totalTaken,
                TotalMissed = totalScheduled - totalTaken,
                AsNeededCount = asNeededCount
            };
        }

        public static double GetAdherenceRate(WeeklyStats stats)
        {
            return stats.TotalScheduled > 0 ? (double)stats.TotalTaken / stats.TotalScheduled * 100 : 0;
        }

        public static string GetAdherenceMessage(double rate)
        {
            return rate switch
            {
                >= 90 => "Excellent adherence!",
                >= 70 => "Good adherence",
                _ => "Needs improvement"
            };
        }

        public static string GetAdherenceColorClass(double rate)
        {
            return rate switch
            {
                >= 90 => "text-success",
                >= 70 => "text-warning",
                _ => "text-danger"
            };
        }
    }
}