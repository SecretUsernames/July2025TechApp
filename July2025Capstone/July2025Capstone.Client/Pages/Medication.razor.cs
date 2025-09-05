using July2025Capstone.Shared;
using July2025Capstone.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

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
    }
}
