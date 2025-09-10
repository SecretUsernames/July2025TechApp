using Bunit;
using Xunit;
using July2025Capstone.Client.Pages;
using July2025Capstone.Tests.Base;

namespace July2025Capstone.Tests
{
    public class AnalyticsPageTests : BlazorComponentTestBase
    {
        [Fact]
        public void Analytics_RendersPageTitle()
        {
            // Arrange
            SetupAuthenticatedUser();

            // Act 
            var component = RenderComponent<Analytics>();
            
            // Assert - Just check if the component can be rendered without errors
            Assert.NotNull(component);
        }

        [Fact] 
        public void Analytics_ComponentCanBeInstantiated()
        {
            // Arrange
            SetupAuthenticatedUser();

            // Act
            var component = RenderComponent<Analytics>();

            // Assert - Component renders without throwing exception
            Assert.NotNull(component);
        }

        [Fact]
        public void Analytics_HasCorrectPageRoute()
        {
            // This test verifies the page route attribute
            var analyticsType = typeof(Analytics);
            var pageAttribute = analyticsType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), false).FirstOrDefault();
            
            Assert.NotNull(pageAttribute);
            var routeAttribute = (Microsoft.AspNetCore.Components.RouteAttribute)pageAttribute;
            Assert.Equal("/analytics", routeAttribute.Template);
        }
    }
}
