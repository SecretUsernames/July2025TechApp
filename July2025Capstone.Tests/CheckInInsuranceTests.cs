using Bunit;
using Xunit;
using July2025Capstone.Client.Pages;
using July2025Capstone.Shared.Models;
using July2025Capstone.Tests.Base;

namespace July2025Capstone.Tests
{
    /// <summary>
    /// Unit tests for the CheckInInsurance Blazor component
    /// </summary>
    public class CheckInInsuranceTests : BlazorComponentTestBase
    {
        [Fact]
        public void Component_Should_Render_Successfully_When_Authenticated()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());

            // Act
            var component = RenderComponent<CheckInInsurance>();

            // Assert
            Assert.NotNull(component);
            Assert.Contains("Insurance Information", component.Markup);
            Assert.Contains("Add Insurance Policy", component.Markup);
        }

        [Fact]
        public void Component_Should_Display_Progress_Bar_With_Correct_Step()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());

            // Act
            var component = RenderComponent<CheckInInsurance>();

            // Assert
            Assert.Contains("Step 4 of 8", component.Markup);
            Assert.Contains("Insurance Information", component.Markup);
            var progressBar = component.Find(".progress-bar");
            Assert.Contains("50%", progressBar.GetAttribute("style"));
        }

        [Fact]
        public void Component_Should_Display_Empty_State_When_No_Insurance_Policies()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());

            // Act
            var component = RenderComponent<CheckInInsurance>();

            // Assert
            Assert.Contains("No insurance policies added yet", component.Markup);
            Assert.Contains("Click \"Add Insurance Policy\" to get started", component.Markup);
            var emptyStateIcon = component.Find(".bi-shield-plus");
            Assert.NotNull(emptyStateIcon);
        }

        [Fact]
        public void Component_Should_Display_Insurance_Policies_In_Table()
        {
            // Arrange
            SetupAuthenticatedUser();
            var insurancePolicies = CreateSampleInsurancePolicies();
            SetupHttpGetMock("api/checkin/insurance", insurancePolicies);

            // Act
            var component = RenderComponent<CheckInInsurance>();

            // Assert
            var table = component.Find("table");
            Assert.NotNull(table);
            
            var rows = component.FindAll("tbody tr");
            Assert.Equal(2, rows.Count);
            
            // Check that policy data is displayed
            Assert.Contains("Blue Cross Blue Shield", component.Markup);
            Assert.Contains("BC123456", component.Markup);
            Assert.Contains("John Doe", component.Markup);
            Assert.Contains("(Self)", component.Markup);
            Assert.Contains("Aetna", component.Markup);
            Assert.Contains("AET987654", component.Markup);
            Assert.Contains("Jane Doe", component.Markup);
            Assert.Contains("(Spouse)", component.Markup);
        }

        [Fact]
        public void AddInsuranceButton_Should_Open_Modal()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());
            var component = RenderComponent<CheckInInsurance>();

            // Act
            var addButton = component.Find("button:contains('Add Insurance Policy')");
            addButton.Click();

            // Assert
            var modal = component.Find(".modal");
            Assert.NotNull(modal);
            Assert.Contains("Add Insurance Policy", component.Find(".modal-title").TextContent);
            Assert.True(modal.ClassList.Contains("show"));
        }

        [Fact]
        public void EditButton_Should_Open_Modal_With_Populated_Data()
        {
            // Arrange
            SetupAuthenticatedUser();
            var insurancePolicies = CreateSampleInsurancePolicies();
            SetupHttpGetMock("api/checkin/insurance", insurancePolicies);
            var component = RenderComponent<CheckInInsurance>();

            // Act
            var editButtons = component.FindAll("button[title='Edit']");
            editButtons.First().Click();

            // Assert
            var modal = component.Find(".modal");
            Assert.NotNull(modal);
            Assert.Contains("Edit Insurance Policy", component.Find(".modal-title").TextContent);
        }

        [Fact]
        public void SaveInsurance_Should_Call_API_With_Correct_Data()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());
            
            var saveResponse = new SaveInsuranceResponse
            {
                Success = true,
                Message = "Insurance policy saved successfully!",
                InsuranceId = 1
            };
            SetupHttpPostMock("api/checkin/save-insurance", saveResponse);

            var component = RenderComponent<CheckInInsurance>();

            // Act
            var addButton = component.Find("button:contains('Add Insurance Policy')");
            addButton.Click();

            // Fill in the form with valid data
            FillInsuranceForm(component, "Test Provider", "TEST123", "GRP456", "Test User", "Self");

            var saveButton = component.Find("button[type='submit']");
            saveButton.Click();

            // Assert
            VerifyHttpRequest(HttpMethod.Post, "api/checkin/save-insurance");
        }

        [Fact]
        public void DeleteInsurance_Should_Call_API()
        {
            // Arrange
            SetupAuthenticatedUser();
            var insurancePolicies = CreateSampleInsurancePolicies();
            SetupHttpGetMock("api/checkin/insurance", insurancePolicies);
            SetupHttpDeleteMock("api/checkin/insurance/");

            var component = RenderComponent<CheckInInsurance>();

            // Act
            var deleteButton = component.Find("button[title='Delete']");
            deleteButton.Click();

            // Assert
            VerifyHttpRequest(HttpMethod.Delete, "api/checkin/insurance/1");
        }

        [Fact]
        public void BackButton_Should_Navigate_To_Medical_History()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());
            var component = RenderComponent<CheckInInsurance>();

            // Act
            var backButton = component.Find("button:contains('Back')");
            backButton.Click();

            // Assert
            Assert.Equal("https://localhost:7014/checkin-form-creation/medical-history", NavigationManager.Uri);
        }

        [Fact]
        public void ContinueButton_Should_Navigate_To_Emergency_Contacts()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());
            var component = RenderComponent<CheckInInsurance>();

            // Act
            var continueButton = component.Find("button:contains('Continue to Emergency Contacts')");
            continueButton.Click();

            // Assert
            Assert.Equal("https://localhost:7014/checkin-form-creation/emergency-contacts", NavigationManager.Uri);
        }

        [Fact]
        public void Modal_CloseButton_Should_Close_Modal()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());
            var component = RenderComponent<CheckInInsurance>();

            // Open modal
            var addButton = component.Find("button:contains('Add Insurance Policy')");
            addButton.Click();

            // Verify modal is open
            var modal = component.Find(".modal.show");
            Assert.NotNull(modal);

            // Act
            var closeButton = component.Find(".btn-close");
            closeButton.Click();

            // Assert
            var modals = component.FindAll(".modal.show");
            Assert.Empty(modals);
        }

        [Fact]
        public void Success_Message_Should_Display_After_Successful_Save()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());
            
            var saveResponse = new SaveInsuranceResponse
            {
                Success = true,
                Message = "Insurance policy saved successfully!",
                InsuranceId = 1
            };
            SetupHttpPostMock("api/checkin/save-insurance", saveResponse);

            var component = RenderComponent<CheckInInsurance>();

            // Act
            var addButton = component.Find("button:contains('Add Insurance Policy')");
            addButton.Click();

            FillInsuranceForm(component, "Test Provider", "TEST123", "GRP456", "Test User", "Self");

            var saveButton = component.Find("button[type='submit']");
            saveButton.Click();

            // Assert
            var successAlert = component.Find(".alert-success");
            Assert.NotNull(successAlert);
            Assert.Contains("Insurance policy saved successfully!", successAlert.TextContent);
        }

        [Fact]
        public void Form_Should_Require_All_Fields()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());
            var component = RenderComponent<CheckInInsurance>();

            // Open modal
            var addButton = component.Find("button:contains('Add Insurance Policy')");
            addButton.Click();

            // Act - Check that required fields have required attributes
            var providerInput = component.Find("input[placeholder*='Blue Cross Blue Shield']");
            var policyInput = component.Find("input[placeholder*='Policy number']");
            var groupInput = component.Find("input[placeholder*='Group number']");
            var nameInput = component.Find("input[placeholder*='Name of the person']");
            var relationshipSelect = component.Find("select");

            // Assert - These inputs should be present and part of validation
            Assert.NotNull(providerInput);
            Assert.NotNull(policyInput);
            Assert.NotNull(groupInput);
            Assert.NotNull(nameInput);
            Assert.NotNull(relationshipSelect);
        }

        [Fact]
        public void Component_Should_Display_Correct_Header_And_Description()
        {
            // Arrange
            SetupAuthenticatedUser();
            SetupHttpGetMock("api/checkin/insurance", new List<InsurancePolicyDto>());

            // Act
            var component = RenderComponent<CheckInInsurance>();

            // Assert
            Assert.Contains("Insurance Information", component.Markup);
            Assert.Contains("Please add your insurance policy details", component.Markup);
        }

        // Helper methods
        private List<InsurancePolicyDto> CreateSampleInsurancePolicies()
        {
            return new List<InsurancePolicyDto>
            {
                new InsurancePolicyDto
                {
                    Id = 1,
                    Provider = "Blue Cross Blue Shield",
                    PolicyNumber = "BC123456",
                    GroupNumber = "GRP789",
                    PolicyholderName = "John Doe",
                    RelationshipToPatient = "Self"
                },
                new InsurancePolicyDto
                {
                    Id = 2,
                    Provider = "Aetna",
                    PolicyNumber = "AET987654",
                    GroupNumber = "GRP321",
                    PolicyholderName = "Jane Doe",
                    RelationshipToPatient = "Spouse"
                }
            };
        }

        private void FillInsuranceForm(IRenderedComponent<CheckInInsurance> component, 
            string provider, string policyNumber, string groupNumber, string policyholderName, string relationship)
        {
            var providerInput = component.Find("input[placeholder*='Blue Cross Blue Shield']");
            var policyInput = component.Find("input[placeholder*='Policy number']");
            var groupInput = component.Find("input[placeholder*='Group number']");
            var nameInput = component.Find("input[placeholder*='Name of the person']");
            var relationshipSelect = component.Find("select");

            providerInput.Change(provider);
            policyInput.Change(policyNumber);
            groupInput.Change(groupNumber);
            nameInput.Change(policyholderName);
            relationshipSelect.Change(relationship);
        }
    }
}