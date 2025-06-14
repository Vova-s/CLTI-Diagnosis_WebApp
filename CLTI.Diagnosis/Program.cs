using CLTI.Diagnosis.Components;
using CLTI.Diagnosis.Components.Account;
using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

// Dodajeme API kontrolery
builder.Services.AddControllers();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddSingleton<StateService>();
builder.Services.AddScoped<CLTI.Diagnosis.Services.CltiCaseService>();

// Dodajeme CORS pre klientsku cast
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7124", "http://localhost:5276")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorClient");
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CLTI.Diagnosis.Client._Imports).Assembly);

// Mapujeme API kontrolery
app.MapControllers();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Add fallback route for handling 404s - this should be LAST
app.MapFallback(async context =>
{
    var path = context.Request.Path.Value ?? "/";

    // Don't handle API calls, static files, etc.
    if (path.StartsWith("/api") ||
        path.StartsWith("/_framework") ||
        path.StartsWith("/css") ||
        path.StartsWith("/js") ||
        path.StartsWith("/Photo") ||
        path.StartsWith("/swagger") ||
        path.Contains("."))
    {
        context.Response.StatusCode = 404;
        return;
    }

    // Redirect to error page with path information
    context.Response.Redirect($"/Error?path={Uri.EscapeDataString(path)}&type=404");
});

app.Run();