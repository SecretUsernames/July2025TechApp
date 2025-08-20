using July2025Capstone.Client.Pages;
using July2025Capstone.Components;
using July2025Capstone.Components.Account;
using July2025Capstone.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Balanced static file caching for better development experience
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
                    // Short cache for CSS/JS to allow Hot Reload but avoid constant downloads
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 minutes
                    ctx.Context.Response.Headers["Content-Type"] = "text/css; charset=utf-8";
                }
                else if (path.EndsWith(".wasm") || path.EndsWith(".dll"))
                {
                    // Longer cache for WASM files (they change less frequently)
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=1800"); // 30 minutes
                }
                else if (path.EndsWith(".pdb") || path.EndsWith(".dat") || path.EndsWith(".json"))
                {
                    // No cache for debug/config files (these need to be fresh)
                    ctx.Context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                }
                else
                {
                    // Default short cache for other static files
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=600"); // 10 minutes
                }

                // Fix MIME types
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
                    // Cache CSS/JS for 1 hour in production
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=3600");
                }
                else if (path.EndsWith(".wasm") || path.EndsWith(".dll"))
                {
                    // Cache WebAssembly files longer (24 hours)
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=86400");
                }
                else if (path.EndsWith(".dat") || path.EndsWith(".pdb"))
                {
                    // Cache debug files for shorter time (1 hour)
                    ctx.Context.Response.Headers.Add("Cache-Control", "public, max-age=3600");
                }
            }
        }
    });
}

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(July2025Capstone.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
