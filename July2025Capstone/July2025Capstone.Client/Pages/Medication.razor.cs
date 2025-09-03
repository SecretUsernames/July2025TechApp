using July2025Capstone.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace July2025Capstone.Client.Pages
{
    public partial class Medication
    {
        [Inject]
        public AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject]
        public HttpClient Http { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        //public UserDTO User { get; set; } = new UserDTO();

        public List<MedicationDTO> medications { get; set; }

        protected override async Task OnInitializedAsync()
        {
            //Ensure that the user is logged in
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if(user.Identity?.IsAuthenticated == true)
            {
                IsAuthenticated = true;
                try
                {
                    //Fetch the user data (UserDTO)
                    //this.User = await Http.GetFromJsonAsync<UserDTO>("api/User");

                    medications = await Http.GetFromJsonAsync<List<MedicationDTO>>("https://localhost:7014/api/medications");
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error fetching medication data: {ex.Message}");
                    //TODO: Add better error handling
                }
            }
        }

        public async Task GetMovies()
        {

        }
    }
}
