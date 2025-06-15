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
using System.Security.Claims;

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

// DB context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// API Key service
builder.Services.AddScoped<ApiKeyService, ApiKeyService>();

// Shared client services
builder.Services.AddSingleton<StateService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<CLTI.Diagnosis.Services.CltiCaseService>();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// ✅ ПОКРАЩЕНА КОНФІГУРАЦІЯ HTTP КЛІЄНТІВ З АВТЕНТИФІКАЦІЄЮ
builder.Services.AddHttpClient("InternalApi", (sp, client) =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    var environment = sp.GetRequiredService<IWebHostEnvironment>();

    string baseUrl;

    if (httpContext != null)
    {
        var request = httpContext.Request;
        baseUrl = $"{request.Scheme}://{request.Host}";

        // ✅ ПОКРАЩЕНА ПЕРЕДАЧА COOKIES
        var cookieHeader = request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            // Очищаємо попередні cookies
            client.DefaultRequestHeaders.Remove("Cookie");
            client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }

        // ✅ ДОДАЄМО ANTIFORGERY TOKEN ЯКЩО ДОСТУПНИЙ
        if (request.Headers.TryGetValue("RequestVerificationToken", out var token))
        {
            client.DefaultRequestHeaders.Add("RequestVerificationToken", token.ToString());
        }

        // ✅ ДОДАЄМО USER INFO ДЛЯ ДЕБАГУ
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                client.DefaultRequestHeaders.Remove("X-User-Id");
                client.DefaultRequestHeaders.Add("X-User-Id", userId);
            }
        }
    }
    else
    {
        if (environment.IsDevelopment())
        {
            baseUrl = "https://localhost:7124";
        }
        else
        {
            baseUrl = "https://antsdemo02.demo.dragon-cloud.org";
        }
    }

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis-Internal");
    client.Timeout = TimeSpan.FromSeconds(30);

    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HttpClient configured - BaseAddress: {BaseUrl}, HasCookies: {HasCookies}",
        baseUrl, httpContext?.Request.Headers.Cookie.Count > 0);

}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = false, // Важливо: вимикаємо автоматичне управління cookies
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

// HttpClient для OpenAI
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis/1.0");
    client.Timeout = TimeSpan.FromSeconds(60);
});

// ✅ DEFAULT HTTP CLIENT
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("InternalApi");
});

// ✅ КЛІЄНТСЬКІ СЕРВІСИ З ПРАВИЛЬНИМ HTTP CLIENT
builder.Services.AddScoped<CltiApiClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("InternalApi");
    return new CltiApiClient(httpClient);
});

builder.Services.AddScoped<CLTI.Diagnosis.Client.Services.CltiCaseService>();

builder.Services.AddScoped<IUserClientService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("InternalApi");
    var logger = sp.GetRequiredService<ILogger<UserClientService>>();
    return new UserClientService(httpClient, logger);
});

builder.Services.AddScoped<IClientApiKeyService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("InternalApi");
    var logger = sp.GetRequiredService<ILogger<ClientApiKeyService>>();
    return new ClientApiKeyService(httpClient, logger);
});

builder.Services.AddScoped<AiChatClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("InternalApi");
    var apiKeyService = sp.GetRequiredService<IClientApiKeyService>();
    var logger = sp.GetRequiredService<ILogger<AiChatClient>>();
    return new AiChatClient(httpClient, apiKeyService, logger);
});

// Логування з більше деталей для діагностики
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();

    if (builder.Environment.IsDevelopment())
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
    else
    {
        logging.SetMinimumLevel(LogLevel.Information); // Тимчасово збільшуємо логування в продакшені
        logging.AddFilter("CLTI.Diagnosis", LogLevel.Information);
        logging.AddFilter("Microsoft.AspNetCore.Http", LogLevel.Information);
        logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Information);
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ✅ ВИПРАВЛЕНА COOKIE AUTHENTICATION - ВИКОРИСТОВУЄМО СТАНДАРТНУ НАЗВУ
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        // ✅ ВИКОРИСТОВУЄМО СТАНДАРТНУ НАЗВУ COOKIE
        // options.Cookie.Name = ".AspNetCore.Identity.Application"; // Це вже за замовчуванням
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;

        // ✅ ПІДТРИМКА BLAZOR SERVER
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/_blazor") ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/_blazor") ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        // ✅ ДОДАЄМО DEBUGGING
        options.Events.OnValidatePrincipal = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Cookie validation - IsAuthenticated: {IsAuth}, Name: {Name}",
                context.Principal?.Identity?.IsAuthenticated ?? false,
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        };
    });

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// ✅ ПОКРАЩЕНА MIDDLEWARE PIPELINE
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

// ✅ Правильний порядок middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();

// ✅ ДОДАЄМО MIDDLEWARE ДЛЯ ЛОГУВАННЯ АВТЕНТИФІКАЦІЇ
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        logger.LogInformation("API Request: {Method} {Path}, IsAuthenticated: {IsAuth}, Cookies: {CookieCount}",
            context.Request.Method,
            context.Request.Path,
            context.User?.Identity?.IsAuthenticated ?? false,
            context.Request.Cookies.Count);
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CLTI.Diagnosis.Client._Imports).Assembly);

app.MapControllers();

// ✅ ПОКРАЩЕНИЙ LOGOUT ENDPOINT
app.MapPost("/Account/Logout", async (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("User logout requested");

    await context.SignOutAsync(IdentityConstants.ApplicationScheme);

    // Очищаємо стандартний cookie
    context.Response.Cookies.Delete(".AspNetCore.Identity.Application");
    context.Response.Cookies.Delete(".AspNetCore.Antiforgery.mYlosc6T-lA");

    logger.LogInformation("User logged out successfully");
    return Results.Redirect("/");
});

// ✅ ДОДАЄМО HEALTH CHECK ENDPOINT
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Environment = app.Environment.EnvironmentName
    });
});

// Fallback route
app.MapFallback(async context =>
{
    var path = context.Request.Path.Value ?? "/";

    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Photo", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
        path.Contains('.', StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not Found");
        return;
    }

    context.Response.Redirect($"/Error?path={Uri.EscapeDataString(path)}&type=404");
});

app.Run();