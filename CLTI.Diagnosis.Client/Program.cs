using CLTI.Diagnosis.Client.Infrastructure.Auth;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using CLTI.Diagnosis.Client.Infrastructure.State;
using CLTI.Diagnosis.Client.Features.Diagnosis.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<StateService>();

// JWT Authentication Services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<JwtAuthenticationStateProvider>());

// Register JWT Authorization Handler
builder.Services.AddTransient<JwtAuthorizationHandler>();

// HTTP Client for API calls with JWT authorization handler
builder.Services.AddHttpClient("InternalApi", client =>
{
    // For Blazor WebAssembly, the client and API are typically on the same server
    // So we use the hosting origin as the base address
    var baseAddress = builder.HostEnvironment.BaseAddress;
    client.BaseAddress = new Uri(baseAddress);
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis-Client");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<JwtAuthorizationHandler>();

// Also configure the default HttpClient with JWT authorization for services that need it
builder.Services.AddHttpClient<HttpClient>(client =>
{
    // For Blazor WebAssembly, the client and API are typically on the same server
    // So we use the hosting origin as the base address
    var baseAddress = builder.HostEnvironment.BaseAddress;
    client.BaseAddress = new Uri(baseAddress);
    client.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis-Client");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<JwtAuthorizationHandler>();

// API Client services
builder.Services.AddScoped<CltiApiClient>();

// Client services for working with CLTI cases
builder.Services.AddScoped<CltiCaseService>();

// AI Client services
builder.Services.AddScoped<IClientApiKeyService, ClientApiKeyService>();
builder.Services.AddScoped<AiChatClient>();

// User Client Service
builder.Services.AddScoped<IUserClientService, UserClientService>();

// JWT Authentication Service
builder.Services.AddScoped<JwtAuthenticationService>();

// Auth API Service для логіну/реєстрації
builder.Services.AddScoped<AuthApiService>();

var app = builder.Build();

// Перевіряємо конфігурацію після створення app
using (var scope = app.Services.CreateScope())
{
    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Final HttpClient BaseAddress verification: {BaseAddress}", httpClient.BaseAddress);
}

// Ініціалізуємо JWT провайдер після завантаження
var authStateProvider = app.Services.GetRequiredService<JwtAuthenticationStateProvider>();
await authStateProvider.InitializeAsync();

// Після того як застосунок став інтерактивним, спробуємо зберегти відкладені токени/користувача
var authApiService = app.Services.GetRequiredService<AuthApiService>();
await authApiService.TryFlushPendingAsync();

await app.RunAsync();