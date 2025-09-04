using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace July2025Capstone.Client.Pages;

public partial class CheckInFormCreation : ComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private bool isLoading = true;
    private bool hasExistingForm = false;
    private bool isAuthenticated = false;
    private bool isEditingForm = false; // Add this to track when edit is in progress
    private bool hasLocalUpdates = false; // Track if we've made local updates
    private bool _hasLoadedCompletionStatus = false; // Prevent repeated API calls
    private DateTime? locallyUpdatedDate = null; // Store the locally updated date
    private CheckinFormSummary? existingFormSummary;
    private StatusMessage? statusMessage;
    private FormCompletionStatus? completionStatus; // Add completion status tracking

    protected override async Task OnInitializedAsync()
    {
        // Check authentication state first
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        
        if (!isAuthenticated)
        {
            // Redirect to login with return URL
            Navigation.NavigateTo($"Account/Login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}", forceLoad: true);
            return;
        }

        // Configure the HttpClient base address if it's not set
        if (string.IsNullOrEmpty(Http.BaseAddress?.ToString()))
        {
            Http.BaseAddress = new Uri("https://localhost:7014/"); // Update port as needed
        }

        // Check if we have a locally updated date stored in session storage
        await RestoreLocalUpdates();
        
        await CheckExistingForm();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Only refresh completion status when specifically returning from editing
        // Use a flag to prevent constant API calls
        if (!firstRender && hasExistingForm && existingFormSummary != null && !_hasLoadedCompletionStatus)
        {
            await LoadCompletionStatus();
            _hasLoadedCompletionStatus = true; // Prevent repeated calls
            StateHasChanged();
        }
    }

    private async Task RestoreLocalUpdates()
    {
        try
        {
            // Try to get the locally stored update timestamp
            var storedDate = await JSRuntime.InvokeAsync<string?>("sessionStorage.getItem", "checkinFormLastUpdated");
            if (!string.IsNullOrEmpty(storedDate) && DateTime.TryParse(storedDate, out var parsedDate))
            {
                // Check if the stored date is recent (within the last 10 minutes)
                if (DateTime.Now.Subtract(parsedDate).TotalMinutes < 10)
                {
                    locallyUpdatedDate = parsedDate;
                    hasLocalUpdates = true;
                    Console.WriteLine($"Restored locally updated date: {locallyUpdatedDate:MMM dd, yyyy HH:mm:ss}");
                }
                else
                {
                    // Clean up old stored date
                    await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "checkinFormLastUpdated");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restoring local updates: {ex.Message}");
        }
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
                var serverData = await response.Content.ReadFromJsonAsync<CheckinFormSummary>();
                
                if (serverData != null)
                {
                    existingFormSummary = serverData;
                    
                    // If we have a locally updated date, use it instead of server date
                    if (hasLocalUpdates && locallyUpdatedDate.HasValue)
                    {
                        existingFormSummary.LastModified = locallyUpdatedDate.Value;
                        Console.WriteLine($"Applied locally updated date: {existingFormSummary.LastModified:MMM dd, yyyy HH:mm:ss}");
                    }

                    // Load completion status if we have a form
                    await LoadCompletionStatus();
                }
                
                hasExistingForm = existingFormSummary != null;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                hasExistingForm = false;
                existingFormSummary = null;
                completionStatus = null;
                hasLocalUpdates = false;
                locallyUpdatedDate = null;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // User is not authenticated, redirect to login
                Navigation.NavigateTo($"Account/Login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}", forceLoad: true);
                return;
            }
            else
            {
                // Handle other errors
                hasExistingForm = false;
                statusMessage = new StatusMessage
                {
                    IsSuccess = false,
                    Message = "Unable to check existing forms. Please try again later."
                };
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
        {
            // Handle authentication errors
            Navigation.NavigateTo($"Account/Login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}", forceLoad: true);
            return;
        }
        catch (Exception ex)
        {
            // Handle network/parsing errors (like the JSON parsing error you mentioned)
            hasExistingForm = false;
            statusMessage = new StatusMessage
            {
                IsSuccess = false,
                Message = "Unable to load form data. Please refresh the page or try again later."
            };
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadCompletionStatus()
    {
        try
        {
            // If we already have completion status and no form updates, don't reload
            if (completionStatus != null && !hasLocalUpdates)
            {
                return;
            }

            if (existingFormSummary != null && int.TryParse(existingFormSummary.Id, out int patientId))
            {
                var response = await Http.GetAsync($"api/checkin/patient/{patientId}");
                
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var patientDetail = await response.Content.ReadFromJsonAsync<PatientDetailResponse>();
                        
                        if (patientDetail?.CompletionStatus != null)
                        {
                            completionStatus = patientDetail.CompletionStatus;
                            // Reset the flag since we successfully loaded fresh data
                            hasLocalUpdates = false;
                        }
                        else
                        {
                            completionStatus = CreateSafeCompletionStatus();
                        }
                    }
                    catch (Exception)
                    {
                        completionStatus = CreateSafeCompletionStatus();
                    }
                }
                else
                {
                    completionStatus = CreateSafeCompletionStatus();
                }
            }
            else
            {
                completionStatus = CreateSafeCompletionStatus();
            }
        }
        catch (Exception)
        {
            completionStatus = CreateSafeCompletionStatus();
        }
    }

    private FormCompletionStatus CreateSafeCompletionStatus()
    {
        return new FormCompletionStatus
        {
            HasPersonalInfo = false,
            HasAddress = false,
            HasInsurance = false,
            HasEmergencyContacts = false,
            HasMedications = false,
            HasAllergies = false,
            HasLifestyle = false,
            HasVisitIntake = false,
            HasConsent = false,
            OverallComplete = false
        };
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
            // Set editing state to show visual feedback
            isEditingForm = true;
            
            // Store the old date for debugging
            var oldDate = existingFormSummary.LastModified;
            
            // Update the LastModified date to current date when user starts editing
            var newDate = DateTime.Now;
            existingFormSummary.LastModified = newDate;
            
            // Store the updated date in session storage and local variables
            locallyUpdatedDate = newDate;
            hasLocalUpdates = true;
            
            // Store in session storage so it persists across navigation
            await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", "checkinFormLastUpdated", newDate.ToString("O"));
            
            // Debug log to console to verify the update
            Console.WriteLine($"Date updated from {oldDate:MMM dd, yyyy HH:mm:ss} to {existingFormSummary.LastModified:MMM dd, yyyy HH:mm:ss}");
            Console.WriteLine($"Stored in session storage: {newDate:O}");
            
            // Trigger UI update to show the new date immediately
            StateHasChanged();
            
            // Add a longer delay to ensure the user sees the updated date
            await Task.Delay(1200);
            
            // Reset editing state before navigation
            isEditingForm = false;
            StateHasChanged();
            
            // Small additional delay to show the "Updated!" badge
            await Task.Delay(300);
            
            // Navigate to edit mode with the existing form ID
            Navigation.NavigateTo($"/checkin-form-creation/personal-info");
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
                
                // Clear the locally stored date since PDF generation might indicate completion
                await ClearLocalUpdates();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // User session expired, redirect to login
                Navigation.NavigateTo($"Account/Login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}", forceLoad: true);
                return;
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
        catch (HttpRequestException ex) when (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
        {
            // Handle authentication errors
            Navigation.NavigateTo($"Account/Login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}", forceLoad: true);
            return;
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

    private async Task ClearLocalUpdates()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "checkinFormLastUpdated");
            hasLocalUpdates = false;
            locallyUpdatedDate = null;
            _hasLoadedCompletionStatus = false; // Allow fresh load next time
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing local updates: {ex.Message}");
        }
    }

    // Add method to refresh completion status when actually needed
    public async Task RefreshCompletionStatus()
    {
        hasLocalUpdates = true; // Force a refresh
        _hasLoadedCompletionStatus = false; // Allow reload
        await LoadCompletionStatus();
        StateHasChanged();
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

    public class FormCompletionStatus
    {
        public bool HasPersonalInfo { get; set; }
        public bool HasAddress { get; set; }
        public bool HasInsurance { get; set; }
        public bool HasEmergencyContacts { get; set; }
        public bool HasMedications { get; set; }
        public bool HasAllergies { get; set; }
        public bool HasLifestyle { get; set; }
        public bool HasVisitIntake { get; set; }
        public bool HasConsent { get; set; }
        public bool OverallComplete { get; set; }
    }

    public class PatientDetailResponse
    {
        public FormCompletionStatus? CompletionStatus { get; set; }
    }
}