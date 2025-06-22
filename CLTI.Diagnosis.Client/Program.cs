using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Client.Services;
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

// HTTP Client for API calls - ВИПРАВЛЕНО
builder.Services.AddScoped(sp =>
{
    // Визначаємо базову адресу напряму
    var baseAddress = builder.HostEnvironment.IsDevelopment()
        ? "https://localhost:7124/"
        : "https://antsdemo02.demo.dragon-cloud.org/";

    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(baseAddress)
    };

    httpClient.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis-Client");
    httpClient.Timeout = TimeSpan.FromSeconds(30);

    // Логування для діагностики
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HttpClient configured with BaseAddress: {BaseAddress} (IsDevelopment: {IsDevelopment})",
        httpClient.BaseAddress, builder.HostEnvironment.IsDevelopment());

    return httpClient;
});

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

await app.RunAsync();