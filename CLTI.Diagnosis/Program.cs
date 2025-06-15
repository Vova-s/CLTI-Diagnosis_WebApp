using CLTI.Diagnosis.Components;
using CLTI.Diagnosis.Components.Account;
using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Client.Services;
using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

// API controllers
builder.Services.AddControllers();

// Identity and auth
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// DB context (додаємо раніше інших сервісів)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// API Key service для читання з БД
builder.Services.AddScoped<ApiKeyService, ApiKeyService>();

// *** ДОДАЄМО AUTHENTICATED HTTP CLIENT SERVICE ***
builder.Services.AddScoped<IAuthenticatedHttpClientService, AuthenticatedHttpClientService>();

// Shared client services
builder.Services.AddSingleton<StateService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<CLTI.Diagnosis.Services.CltiCaseService>();

// HttpContextAccessor (required for dynamic base URLs)
builder.Services.AddHttpContextAccessor();

// Register named HttpClients with cookie support
builder.Services.AddHttpClient("Default", (sp, client) =>
{
    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    if (httpContext != null)
    {
        var request = httpContext.Request;
        client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}");

        // Передаємо cookies для автентифікації
        var cookieHeader = request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }
    }
    else
    {
        client.BaseAddress = new Uri("https://localhost:7124");
    }
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = false // Вимикаємо автоматичне управління cookies
});

// HttpClient для OpenAI налаштований правильно
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis/1.0");
    client.Timeout = TimeSpan.FromSeconds(60); // Збільшуємо таймаут до 60 секунд
});

// Register default HttpClient for server-side components
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("Default");
});

// Register CltiApiClient with proper HttpClient
builder.Services.AddScoped<CltiApiClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("Default");
    return new CltiApiClient(httpClient);
});

// Register client-side CltiCaseService
builder.Services.AddScoped<CLTI.Diagnosis.Client.Services.CltiCaseService>();

// *** ДОДАЄМО КЛІЄНТСЬКІ СЕРВІСИ ДЛЯ СЕРВЕРНОЇ СТОРОНИ ***
builder.Services.AddScoped<IUserClientService, UserClientService>();
builder.Services.AddScoped<IClientApiKeyService, ClientApiKeyService>();

// Register AiChatClient для клієнтських компонентів
builder.Services.AddScoped<AiChatClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("Default"); // Використовуємо Default client для звернення до нашого API
    var apiKeyService = sp.GetRequiredService<IClientApiKeyService>();
    var logger = sp.GetRequiredService<ILogger<AiChatClient>>();
    return new AiChatClient(httpClient, apiKeyService, logger);
});

// Додайте логування для діагностики
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    if (builder.Environment.IsDevelopment())
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
});

// CORS
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

// Simple Cookie Authentication (замість повного Identity)
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

// Add minimal Identity services for compatibility
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Middleware pipeline
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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CLTI.Diagnosis.Client._Imports).Assembly);

app.MapControllers();

// Simple logout endpoint
app.MapPost("/Account/Logout", async (HttpContext context) =>
{
    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
    return Results.Redirect("/");
});

// Fallback route
app.MapFallback(async context =>
{
    var path = context.Request.Path.Value ?? "/";
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

    context.Response.Redirect($"/Error?path={Uri.EscapeDataString(path)}&type=404");
});

app.Run();