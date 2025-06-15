using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<StateService>();
builder.Services.AddAuthenticationStateDeserialization();

// Додаємо HTTP клієнт для API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CltiApiClient>();

// Додаємо клієнтський сервіс для роботи з CLTI cases
builder.Services.AddScoped<CltiCaseService>();

// *** НОВИЙ КОД: Додаємо клієнтські AI сервіси ***
builder.Services.AddScoped<IClientApiKeyService, ClientApiKeyService>();
builder.Services.AddScoped<AiChatClient>();

await builder.Build().RunAsync();