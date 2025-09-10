using System.Security.Claims;
using Syncfusion.Blazor;
using July2025Capstone.Client.Pages;
using July2025Capstone.Components;
using July2025Capstone.Components.Account;
using July2025Capstone.Data;
using July2025Capstone.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// ===== helper to seed Admin user/role =====
static async Task SeedAdminUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    const string adminEmail = "admin@admin.com";
    const string adminRole = "Admin";
    const string tempPassword = "AdminPass123!"; // change after first login

    // ensure Admin role exists
    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole(adminRole));
        if (!roleResult.Succeeded)
            throw new Exception("Failed to create Admin role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
    }

    // find or create the admin user
    var user = await userManager.FindByEmailAsync(adminEmail);
    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var create = await userManager.CreateAsync(user, tempPassword);
        if (!create.Succeeded)
            throw new Exception("Failed to create admin user: " + string.Join(", ", create.Errors.Select(e => e.Description)));
    }

    // add to Admin role
    if (!await userManager.IsInRoleAsync(user, adminRole))
    {
        var add = await userManager.AddToRoleAsync(user, adminRole);
        if (!add.Succeeded)
            throw new Exception("Failed to add user to Admin role: " + string.Join(", ", add.Errors.Select(e => e.Description)));
    }

    // ensure a Role claim exists
    var claims = await userManager.GetClaimsAsync(user);
    if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == adminRole))
    {
        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, adminRole));
    }
}

// ===== app builder =====
var builder = WebApplication.CreateBuilder(args);

// Razor/Blazor
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddSyncfusionBlazor();

// API controllers
builder.Services.AddControllers();

// HttpClient (SSR) - let ASP.NET set BaseAddress
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

// Identity Core (WITH ROLES)
builder.Services
    .AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ensure role claims are created
builder.Services.AddScoped<
    IUserClaimsPrincipalFactory<ApplicationUser>,
    UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>>();

// OpenAI client
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    var key = builder.Configuration["OpenAI:ApiKey"];
    if (!string.IsNullOrWhiteSpace(key))
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
    }
    client.Timeout = TimeSpan.FromSeconds(60);
});

// PDF Generation Service
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// update LastLoginUtc on sign-in
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnSignedIn = async ctx =>
    {
        if (ctx.Principal is null) return;
        var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(ctx.Principal);
        if (user is not null)
        {
            user.LastLoginUtc = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }
    };
});

// ===== build app =====
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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

// ===== seed admin user/role =====
await SeedAdminUserAsync(app.Services);

app.Run();
