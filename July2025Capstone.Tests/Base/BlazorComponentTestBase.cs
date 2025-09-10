using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace July2025Capstone.Tests.Base
{
    /// <summary>
    /// Base class for Blazor component tests that provides common setup and helper methods
    /// </summary>
    public abstract class BlazorComponentTestBase : TestContext, IDisposable
    {
        protected readonly Mock<HttpMessageHandler> MockHttpMessageHandler;
        protected readonly HttpClient HttpClient;
        protected readonly Mock<AuthenticationStateProvider> MockAuthProvider;
        protected readonly MockNavigationManager NavigationManager;

        protected BlazorComponentTestBase()
        {
            MockHttpMessageHandler = new Mock<HttpMessageHandler>();
            HttpClient = new HttpClient(MockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost:7014/")
            };

            MockAuthProvider = new Mock<AuthenticationStateProvider>();
            NavigationManager = new MockNavigationManager("https://localhost:7014/");

            // Register common services
            Services.AddSingleton(HttpClient);
            Services.AddSingleton(MockAuthProvider.Object);
            Services.AddSingleton<NavigationManager>(NavigationManager);
        }

        /// <summary>
        /// Sets up an authenticated user for testing
        /// </summary>
        /// <param name="userName">The username for the authenticated user</param>
        /// <param name="userId">The user ID for the authenticated user</param>
        protected void SetupAuthenticatedUser(string userName = "test@example.com", string userId = "test-user-id")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "test");

            var claimsPrincipal = new ClaimsPrincipal(identity);
            var authState = Task.FromResult(new AuthenticationState(claimsPrincipal));

            MockAuthProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
        }

        /// <summary>
        /// Sets up an unauthenticated user for testing
        /// </summary>
        protected void SetupUnauthenticatedUser()
        {
            var identity = new ClaimsIdentity();
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var authState = Task.FromResult(new AuthenticationState(claimsPrincipal));

            MockAuthProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
        }

        /// <summary>
        /// Sets up an HTTP mock for a successful GET request
        /// </summary>
        /// <typeparam name="T">The type of response data</typeparam>
        /// <param name="endpoint">The API endpoint (relative to base URL)</param>
        /// <param name="responseData">The data to return</param>
        protected void SetupHttpGetMock<T>(string endpoint, T responseData)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseData)
            };

            MockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri.ToString().Contains(endpoint)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up an HTTP mock for a successful POST request
        /// </summary>
        /// <typeparam name="T">The type of response data</typeparam>
        /// <param name="endpoint">The API endpoint (relative to base URL)</param>
        /// <param name="responseData">The data to return</param>
        protected void SetupHttpPostMock<T>(string endpoint, T responseData)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseData)
            };

            MockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri.ToString().Contains(endpoint)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up an HTTP mock for a successful DELETE request
        /// </summary>
        /// <param name="endpoint">The API endpoint pattern (relative to base URL)</param>
        protected void SetupHttpDeleteMock(string endpoint)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            MockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Delete && 
                        req.RequestUri.ToString().Contains(endpoint)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up an HTTP mock to return a server error
        /// </summary>
        /// <param name="endpoint">The API endpoint (relative to base URL)</param>
        /// <param name="method">The HTTP method</param>
        /// <param name="statusCode">The error status code to return</param>
        protected void SetupHttpErrorMock(string endpoint, HttpMethod method, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            var response = new HttpResponseMessage(statusCode);

            MockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == method && 
                        req.RequestUri.ToString().Contains(endpoint)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up an HTTP mock to return validation errors
        /// </summary>
        /// <param name="endpoint">The API endpoint (relative to base URL)</param>
        /// <param name="validationErrors">Dictionary of field names and error messages</param>
        protected void SetupHttpValidationErrorMock(string endpoint, Dictionary<string, string[]> validationErrors)
        {
            var errorResponse = new
            {
                title = "Validation failed",
                errors = validationErrors
            };

            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(errorResponse)
            };

            MockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri.ToString().Contains(endpoint)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Verifies that an HTTP request was made with the specified method and endpoint
        /// </summary>
        /// <param name="method">The expected HTTP method</param>
        /// <param name="endpoint">The expected endpoint</param>
        /// <param name="times">How many times the request should have been made</param>
        protected void VerifyHttpRequest(HttpMethod method, string endpoint, Times? times = null)
        {
            MockHttpMessageHandler.Protected()
                .Verify("SendAsync", times ?? Times.AtLeastOnce(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == method &&
                        req.RequestUri.ToString().Contains(endpoint)),
                    ItExpr.IsAny<CancellationToken>());
        }

        public new virtual void Dispose()
        {
            HttpClient?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Mock NavigationManager for testing navigation functionality
    /// </summary>
    public class MockNavigationManager : NavigationManager
    {
        public MockNavigationManager(string baseUri = "https://localhost:7014/") : base()
        {
            Initialize(baseUri, baseUri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}