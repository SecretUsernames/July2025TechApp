using July2025Capstone.Shared;
using July2025Capstone.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;


namespace July2025Capstone.Client.Pages
{
    public partial class Medication
    {
        private List<MedicationDto> medications = new();
        private MedicationDto currentMedication = new();
        private bool showDialog = false;
        private string dialogTitle = "Add Medication";
        private bool isLoading = false;
        private bool isAuthenticated = false;
        private string? errorMessage;
        private string? successMessage;

        // Tracking fields
        private List<MedicationDose> doses = new();
        private WeeklyStats weeklyStats = new();
        private double adherenceRate = 0;

        private List<DropDownOption<DosageUnit>> dosageUnits = new()
        {
            new() { Text = "mg (Milligrams)", Value = DosageUnit.Milligrams },
            new() { Text = "mcg (Micrograms)", Value = DosageUnit.Micrograms },
            new() { Text = "g (Grams)", Value = DosageUnit.Grams },
            new() { Text = "mL (Milliliters)", Value = DosageUnit.Milliliters },
            new() { Text = "L (Liters)", Value = DosageUnit.Liters },
            new() { Text = "IU (International Units)", Value = DosageUnit.Units },
            new() { Text = "Other", Value = DosageUnit.Other }
        };

        private List<DropDownOption<MedicationFrequency>> frequencies = new()
        {
            new() { Text = "Once daily", Value = MedicationFrequency.OnceDaily },
            new() { Text = "Twice daily", Value = MedicationFrequency.TwiceDaily },
            new() { Text = "Three times daily", Value = MedicationFrequency.ThreeDaily },
            new() { Text = "Four times daily", Value = MedicationFrequency.FourDaily },
            new() { Text = "As needed", Value = MedicationFrequency.AsNeeded }
        };

        public class DropDownOption<T>
        {
            public string Text { get; set; } = "";
            public T Value { get; set; } = default!;
        }

        protected override async Task OnInitializedAsync()
        {
            // Check authentication state
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            if (!isAuthenticated)
            {
                return;
            }

            // Configure HttpClient base address
            if (string.IsNullOrEmpty(Http.BaseAddress?.ToString()))
            {
                Http.BaseAddress = new Uri("https://localhost:7014/");
            }

            await LoadMedications();
            LoadDosesAsync();
            UpdateStats();
        }

        private async Task LoadMedications()
        {
            try
            {
                isLoading = true;
                var response = await Http.GetAsync("api/checkin/medications");

                if (response.IsSuccessStatusCode)
                {
                    var medicationList = await response.Content.ReadFromJsonAsync<List<MedicationDto>>();
                    medications = medicationList ?? new List<MedicationDto>();
                    LoadDosesAsync();
                    UpdateStats();
                    StateHasChanged();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    errorMessage = "You are not authorized to access this data. Please log in again.";
                    isAuthenticated = false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading medications: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void ShowAddDialog()
        {
            currentMedication = new MedicationDto();
            dialogTitle = "Add Medication";
            showDialog = true;
            ClearMessages();
        }

        private void EditMedication(MedicationDto? medication)
        {
            if (medication != null)
            {
                currentMedication = new MedicationDto
                {
                    Id = medication.Id,
                    Name = medication.Name,
                    DosageStrength = medication.DosageStrength,
                    DosageUnit = medication.DosageUnit,
                    CustomDosageUnit = medication.CustomDosageUnit,
                    Frequency = medication.Frequency
                };
                dialogTitle = "Edit Medication";
                showDialog = true;
                ClearMessages();
            }
        }

        private async Task DeleteMedication(MedicationDto? medication)
        {
            if (medication != null && medication.Id > 0)
            {
                try
                {
                    ClearMessages();
                    var response = await Http.DeleteAsync($"api/checkin/medication/{medication.Id}");

                    if (response.IsSuccessStatusCode)
                    {
                        successMessage = "Medication deleted successfully!";
                        await LoadMedications();
                        LoadDosesAsync();
                        UpdateStats();
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        errorMessage = $"Error deleting medication: {errorContent}";
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error deleting medication: {ex.Message}";
                }
            }
        }

        private async Task SaveMedication()
        {
            try
            {
                isLoading = true;
                ClearMessages();

                var request = new SaveMedicationRequest { Medication = currentMedication };
                var response = await Http.PostAsJsonAsync("api/checkin/save-medication", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<SaveMedicationResponse>();
                    if (result?.Success == true)
                    {
                        successMessage = "Medication saved successfully!";
                        showDialog = false;
                        await LoadMedications();
                        LoadDosesAsync();
                        UpdateStats();
                    }
                    else
                    {
                        errorMessage = result?.Message ?? "Failed to save medication";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    errorMessage = $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving medication: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void CloseDialog()
        {
            showDialog = false;
            ClearMessages();
        }

        private void ClearMessages()
        {
            errorMessage = null;
            successMessage = null;
        }

        private void GoBack()
        {
            Navigation.NavigateTo("/checkin-form-creation/personal-info");
        }

        private void Continue()
        {
            Navigation.NavigateTo("/checkin-form-creation/medical-history");
        }

        // Tracking methods
        /*
        private void LoadDoses()
        {
            doses = MedicationTracker.InitializeDoses(medications, doses);
        }
        */

        private async Task LoadDosesAsync()
        {
            doses = new List<MedicationDose>();

            foreach (var medication in medications)
            {
                try
                {
                    var medDoses = await Http.GetFromJsonAsync<List<MedicationDose>>(
                        $"api/MedicationDose/medication/{medication.Id}");

                    if (medDoses != null)
                    {
                        doses.AddRange(medDoses);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading doses for medication {medication.Id}: {ex.Message}");
                }
            }

            UpdateStats();
        }

        private async Task LoadDosesAsync(int medicationId)
        {
            try
            {
                var medDoses = await Http.GetFromJsonAsync<List<MedicationDose>>(
                    $"api/medicationdose/medication/{medicationId}");

                if (medDoses != null)
                {
                    // Replace existing doses for this medication
                    doses.RemoveAll(d => d.MedicationId == medicationId);
                    doses.AddRange(medDoses);
                }

                UpdateStats();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading doses for medication {medicationId}: {ex.Message}");
            }
        }


        private void UpdateStats()
        {
            weeklyStats = MedicationTracker.CalculateWeeklyStats(medications, doses);
            adherenceRate = MedicationTracker.GetAdherenceRate(weeklyStats);
        }

        /*
        private void ToggleDose(int medicationId, int dayOfWeek, TimeOfDay timeOfDay)
        {
            var dose = doses.FirstOrDefault(d =>
                d.MedicationId == medicationId &&
                d.DayOfWeek == dayOfWeek &&
                d.TimeOfDay == timeOfDay);

            if (dose != null)
            {
                dose.Taken = !dose.Taken;
                dose.TakenAt = dose.Taken ? DateTime.Now : null;
                UpdateStats();
                StateHasChanged();
            }
        }
        */
        private async Task ToggleDoseAsync(int medicationId, int dayOfWeek, TimeOfDay timeOfDay)
        {
            try
            {
                var request = new ToggleDoseRequest
                {
                    MedicationId = medicationId,
                    DayOfWeek = dayOfWeek,
                    TimeOfDay = timeOfDay
                };

                var response = await Http.PostAsJsonAsync("api/medicationdose/toggle", request);

                if (response.IsSuccessStatusCode)
                {
                    // Refresh doses after toggling
                    await LoadDosesAsync(medicationId);
                    UpdateStats();
                    StateHasChanged();
                }
                else
                {
                    Console.WriteLine("Failed to toggle dose.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error calling API for medication dose.");
                throw;
            }
           
        }

        /*
        private MedicationDose? GetDoseStatus(int medicationId, int dayOfWeek, TimeOfDay timeOfDay)
        {
            return doses.FirstOrDefault(d =>
                d.MedicationId == medicationId &&
                d.DayOfWeek == dayOfWeek &&
                d.TimeOfDay == timeOfDay);
        }
        */

        private MedicationDose? GetDoseStatus(int medicationId, int dayOfWeek, TimeOfDay timeOfDay)
        {
            return doses.FirstOrDefault(d =>
                d.MedicationId == medicationId &&
                d.DayOfWeek == dayOfWeek &&
                d.TimeOfDay == timeOfDay);
        }

        private string GetAdherenceColorClass(double rate)
        {
            return rate switch
            {
                >= 90 => "bg-success text-success",
                >= 70 => "bg-warning text-warning",
                _ => "bg-danger text-danger"
            };
        }

        private string GetProgressBarClass(double rate)
        {
            return rate switch
            {
                >= 90 => "bg-success",
                >= 70 => "bg-warning",
                _ => "bg-danger"
            };
        }
    }
}