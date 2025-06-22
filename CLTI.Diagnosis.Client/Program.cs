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
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<AuthApiService>();
// HTTP Client for API calls
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    httpClient.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis-Client");
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

await builder.Build().RunAsync();