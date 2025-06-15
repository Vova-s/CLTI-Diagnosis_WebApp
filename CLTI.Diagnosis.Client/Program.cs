using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<StateService>();
builder.Services.AddAuthenticationStateDeserialization();

// ✅ OPTION 1: Try the standard HttpClient approach for .NET 9 (RECOMMENDED)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// ✅ OPTION 2: If you need credentials support, use this WebAssemblyHttpHandler approach
/*
builder.Services.AddScoped(sp =>
    new HttpClient(new WebAssemblyHttpHandler())
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });
*/

// ✅ OPTION 3: If you need to include credentials, use this approach (advanced)

builder.Services.AddScoped(sp =>
{
    var handler = new WebAssemblyHttpHandler();
    // Set credentials at runtime via fetch options if needed
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };
});


builder.Services.AddScoped<CltiApiClient>();

// Додаємо клієнтський сервіс для роботи з CLTI cases
builder.Services.AddScoped<CltiCaseService>();

// *** НОВИЙ КОД: Додаємо клієнтські AI сервіси ***
builder.Services.AddScoped<IClientApiKeyService, ClientApiKeyService>();
builder.Services.AddScoped<AiChatClient>();

// *** ДОДАЄМО USER CLIENT SERVICE ***
builder.Services.AddScoped<IUserClientService, UserClientService>();

await builder.Build().RunAsync();