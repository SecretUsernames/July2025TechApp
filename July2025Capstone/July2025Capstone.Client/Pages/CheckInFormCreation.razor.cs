using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace July2025Capstone.Client.Pages;

public partial class CheckInFormCreation : ComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool isLoading = true;
    private bool hasExistingForm = false;
    private CheckinFormSummary? existingFormSummary;
    private StatusMessage? statusMessage;

    protected override async Task OnInitializedAsync()
    {
        // Configure the HttpClient base address if it's not set
        if (string.IsNullOrEmpty(Http.BaseAddress?.ToString()))
        {
            Http.BaseAddress = new Uri("https://localhost:7014/"); // Update port as needed
        }

        await CheckExistingForm();
    }

    private async Task CheckExistingForm()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            // Check if user has existing check-in forms
            var response = await Http.GetAsync("api/checkin/user-form");
            
            if (response.IsSuccessStatusCode)
            {
                existingFormSummary = await response.Content.ReadFromJsonAsync<CheckinFormSummary>();
                hasExistingForm = existingFormSummary != null;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                hasExistingForm = false;
                existingFormSummary = null;
            }
            else
            {
                // Handle error - for now, assume no form exists
                hasExistingForm = false;
                statusMessage = new StatusMessage
                {
                    IsSuccess = false,
                    Message = "Unable to check existing forms. Please try again later."
                };
            }
        }
        catch (Exception ex)
        {
            // Handle network errors
            hasExistingForm = false;
            statusMessage = new StatusMessage
            {
                IsSuccess = false,
                Message = $"Error loading form status: {ex.Message}"
            };
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task StartFormCreation()
    {
        // Navigate to the form creation wizard
        Navigation.NavigateTo("/checkin-form-creation/personal-info");
    }

    private async Task EditForm()
    {
        if (existingFormSummary != null)
        {
            // Navigate to edit mode with the existing form ID
            Navigation.NavigateTo($"/checkin-form-creation/edit/{existingFormSummary.Id}");
        }
    }

    private async Task GenerateCheckInPdf()
    {
        if (existingFormSummary == null) return;

        isLoading = true;
        statusMessage = null;
        StateHasChanged();

        try
        {
            var response = await Http.GetAsync($"api/checkin/generate-pdf/{existingFormSummary.Id}");
            
            if (response.IsSuccessStatusCode)
            {
                // Download the PDF
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"CheckInForm_{DateTime.Now:yyyyMMdd}.pdf";
                
                await JSRuntime.InvokeVoidAsync("downloadFile", fileName, "application/pdf", pdfBytes);
                
                statusMessage = new StatusMessage
                {
                    IsSuccess = true,
                    Message = "Check-in PDF generated successfully!"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                statusMessage = new StatusMessage
                {
                    IsSuccess = false,
                    Message = $"Failed to generate PDF: {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            statusMessage = new StatusMessage
            {
                IsSuccess = false,
                Message = $"Error generating PDF: {ex.Message}"
            };
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    // Model classes
    public class CheckinFormSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsComplete { get; set; }
    }

    public class StatusMessage
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}