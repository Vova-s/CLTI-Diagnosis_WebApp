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

var builder = WebApplication.CreateBuilder(args);

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

// JWT CONFIGURATION - PURE JWT, NO COOKIES
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-min-256-bits-long-for-security-purposes-12345";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CLTI.Diagnosis";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CLTI.Diagnosis.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            // Get token from Authorization header
            var token = context.Request.Headers.Authorization
                .FirstOrDefault()?.Replace("Bearer ", "");

            // Try query string for SignalR
            if (string.IsNullOrEmpty(token))
            {
                token = context.Request.Query["access_token"];
            }

            // Try custom header
            if (string.IsNullOrEmpty(token))
            {
                token = context.Request.Headers["X-Access-Token"];
            }

            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            // For API requests return JSON instead of HTML redirect
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true))
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var response = new { error = "Unauthorized", message = "JWT token required" };
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                return context.Response.WriteAsync(json);
            }

            // For web pages redirect to login
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/login");
                context.HandleResponse();
            }

            return Task.CompletedTask;
        }
    };
});

// AUTHORIZATION
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// JWT AUTHENTICATION STATE PROVIDER
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

// HTTP CLIENT FOR BLAZOR WEBASSEMBLY WITH JWT
builder.Services.AddScoped(sp =>
{
    var tokenService = sp.GetRequiredService<JwtTokenService>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();

    var httpClient = new HttpClient();

    string baseUrl;
    if (environment.IsDevelopment())
    {
        baseUrl = "https://localhost:7124";
    }
    else
    {
        baseUrl = "https://antsdemo02.demo.dragon-cloud.org";
    }

    httpClient.BaseAddress = new Uri(baseUrl);
    httpClient.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis-Client");

    return httpClient;
});

// CLIENT SERVICES WITH JWT
builder.Services.AddScoped<CltiApiClient>();
builder.Services.AddScoped<CLTI.Diagnosis.Client.Services.CltiCaseService>();
builder.Services.AddScoped<IUserClientService, UserClientService>();
builder.Services.AddScoped<IClientApiKeyService, ClientApiKeyService>();
builder.Services.AddScoped<AiChatClient>();

// Logging
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
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddFilter("CLTI.Diagnosis", LogLevel.Information);
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

var app = builder.Build();

// MIDDLEWARE PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

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

// CONTROLLERS WITH JWT AUTHORIZATION
app.MapControllers();

// HEALTH CHECK
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
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not Found");
        return;
    }

    context.Response.Redirect($"/Error?path={Uri.EscapeDataString(path)}&type=404");
});

app.Run();