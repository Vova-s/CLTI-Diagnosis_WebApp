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

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddSingleton<StateService>();
builder.Services.AddScoped<CLTI.Diagnosis.Services.CltiCaseService>();


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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CLTI.Diagnosis.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Endpoint to persist current algorithm state
app.MapPost("/api/clticase/save", async (CLTI.Diagnosis.Services.CltiCaseService service, StateService state) =>
{
    await service.SaveCaseAsync(state);
    return Results.Ok();
});

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
        path.Contains("."))
    {
        context.Response.StatusCode = 404;
        return;
    }

    // Redirect to error page with path information
    context.Response.Redirect($"/Error?path={Uri.EscapeDataString(path)}&type=404");
});

app.Run();