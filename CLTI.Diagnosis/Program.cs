// /app/CLTI.Diagnosis/Program.cs
using CLTI.Diagnosis.Components;
using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Client.Services;
using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Services;
using CLTI.Diagnosis.Middleware;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;

 // Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "C:\\inetpub\\CLTI\\logs\\app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] [{ThreadId}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .WriteTo.File(
        path: "C:\\inetpub\\CLTI\\logs\\errors-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        restrictedToMinimumLevel: LogEventLevel.Warning,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] [{ThreadId}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

try
{
    Log.Information("Starting CLTI.Diagnosis application");

var builder = WebApplication.CreateBuilder(args);

// Add Serilog to the application
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName());


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// API controllers
builder.Services.AddControllers();

// DB context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT CONFIGURATION - ONLY JWT AUTHENTICATION
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-min-256-bits-long-for-security-purposes-12345";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CLTI.Diagnosis";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CLTI.Diagnosis.Client";

builder.Services.AddAuthentication(options =>
{
    // JWT as the default and only authentication scheme
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/_blazor"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Authentication failed: {Error}, Path: {Path}, RemoteIp: {RemoteIp}", 
                context.Exception.Message, 
                context.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress);
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Challenge triggered for path: {Path}, Error: {Error}, RemoteIp: {RemoteIp}", 
                context.Request.Path,
                context.Error,
                context.HttpContext.Connection.RemoteIpAddress);
            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("JWT Token validated successfully for user: {User}, Path: {Path}", 
                context.Principal?.Identity?.Name,
                context.Request.Path);
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireDoctorRole", policy => policy.RequireRole("Doctor", "Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Doctor", "Admin"));
});

// Cascading authentication state for Blazor
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

// Application services
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddSingleton<StateService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<CLTI.Diagnosis.Services.CltiCaseService>();
builder.Services.AddScoped<JwtTokenService>();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// HttpClient for OpenAI
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis/1.0");
    client.Timeout = TimeSpan.FromSeconds(60);
});

// HTTP CLIENT for internal API requests (no cookies, JWT only)
builder.Services.AddHttpClient("InternalApi", (sp, client) =>
{
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    string? baseUrl = null;

    if (environment.IsDevelopment())
    {
        // Prefer HTTPS if provided, otherwise fallback to HTTP
        var https = configuration["InternalApi:BaseUrlHttps"];
        var http = configuration["InternalApi:BaseUrlHttp"];
        baseUrl = !string.IsNullOrWhiteSpace(https) ? https : http;
    }
    else
    {
        baseUrl = configuration["InternalApi:BaseUrl"];
    }

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        // As a last resort, try to use HTTPS localhost to avoid null BaseAddress
        baseUrl = environment.IsDevelopment() ? "https://localhost:7124" : "https://localhost";
        logger.LogWarning("InternalApi BaseUrl not configured. Falling back to {Fallback}", baseUrl);
    }

    logger.LogInformation("Configuring InternalApi HttpClient with base URL: {BaseUrl}", baseUrl);

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis-Client");

}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = false, // Disable cookies - using JWT only
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
});

// Default HTTP Client for Blazor (JWT-based)
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("InternalApi");
});

// Client services
builder.Services.AddScoped<CltiApiClient>();
builder.Services.AddScoped<CLTI.Diagnosis.Client.Services.CltiCaseService>();
builder.Services.AddScoped<IUserClientService, UserClientService>();
builder.Services.AddScoped<IClientApiKeyService, ClientApiKeyService>();
builder.Services.AddScoped<AiChatClient>();
builder.Services.AddScoped<AuthApiService>();

// Logging - Now handled by Serilog
// The old AddLogging configuration is replaced by Serilog

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



var app = builder.Build();

// Apply pending EF Core migrations at startup to keep DB schema in sync
try
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    logger.LogInformation("Applying database migrations (if any) ...");
    db.Database.Migrate();
    logger.LogInformation("Database migrations applied successfully.");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to apply database migrations on startup");
    // Proceeding without crashing; the app may still run, but endpoints depending on migrations may fail.
}

// Log application startup
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Application starting. Environment: {Environment}, MachineName: {MachineName}", 
    app.Environment.EnvironmentName, 
    Environment.MachineName);

// MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    startupLogger.LogInformation("Development mode: WebAssembly debugging enabled");
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    startupLogger.LogInformation("Production mode: Exception handler and HSTS enabled");
}

// Add custom middleware for logging
app.UseRequestLogging();
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CLTI.Diagnosis.Client._Imports).Assembly);

// API Controllers
app.MapControllers();

// Health check endpoint
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
        path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
        path.Contains('.', StringComparison.OrdinalIgnoreCase))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("404 Not Found: {Path} from {RemoteIp}", path, context.Connection.RemoteIpAddress);
        
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not Found");
        return;
    }

    context.Response.Redirect($"/Error?path={Uri.EscapeDataString(path)}&type=404");
});

startupLogger.LogInformation("Application started successfully. Listening on configured URLs");

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down CLTI.Diagnosis application");
    Log.CloseAndFlush();
}
