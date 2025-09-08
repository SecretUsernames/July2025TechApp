using Syncfusion.Blazor;
using July2025Capstone.Client.Pages;
using July2025Capstone.Components;
using July2025Capstone.Components.Account;
using July2025Capstone.Data;
using July2025Capstone.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor/Blazor
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddSyncfusionBlazor();

// API controllers
builder.Services.AddControllers();

// HttpClient (SSR)
builder.Services.AddHttpClient();

// CORS (single dev policy)
const string DevCors = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCors, policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Identity/Auth
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

// DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity Core
builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// OpenAI client
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    var key = builder.Configuration["OpenAI:ApiKey"]; // user-secrets or env var
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// PDF Generation Service
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Build
var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Only force HTTPS outside dev
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS for API
app.UseCors(DevCors);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseStaticFiles();

// API routes; disable antiforgery for controllers
app.MapControllers().DisableAntiforgery();

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(July2025Capstone.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

app.Run();
