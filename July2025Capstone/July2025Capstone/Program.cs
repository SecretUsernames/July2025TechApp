using July2025Capstone.Client.Pages;
using July2025Capstone.Components;
using July2025Capstone.Components.Account;
using July2025Capstone.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container BEFORE building the app
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add API controllers
builder.Services.AddControllers();

// Add HttpClient for server-side rendering (needed for SSR prerendering)
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Build the app AFTER all services are registered
var app = builder.Build();

// Configure the HTTP request pipeline
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

app.UseHttpsRedirection();

// Static file configuration
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.Context.Request.Path.Value?.ToLower();
            if (path != null)
            {
                if (path.EndsWith(".css") || path.EndsWith(".js"))
                {
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=300");
                    ctx.Context.Response.Headers["Content-Type"] = "text/css; charset=utf-8";
                }
                else if (path.EndsWith(".wasm") || path.EndsWith(".dll"))
                {
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=1800");
                }
                else if (path.EndsWith(".pdb") || path.EndsWith(".dat") || path.EndsWith(".json"))
                {
                    ctx.Context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                }
                else
                {
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=600");
                }

                if (path.EndsWith(".wasm"))
                {
                    ctx.Context.Response.Headers.Add("Content-Type", "application/wasm");
                }
            }
        }
    });
}
else
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.Context.Request.Path.Value?.ToLower();
            if (path != null)
            {
                if (path.EndsWith(".css") || path.EndsWith(".js"))
                {
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=3600");
                }
                else if (path.EndsWith(".wasm") || path.EndsWith(".dll"))
                {
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=86400");
                }
                else if (path.EndsWith(".dat") || path.EndsWith(".pdb"))
                {
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=3600");
                }
            }
        }
    });
}

// Use CORS
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Add API controllers routing
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(July2025Capstone.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

app.Run();
