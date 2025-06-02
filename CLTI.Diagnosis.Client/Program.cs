using CLTI.Diagnosis.Client.Algoritm;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<StateService>(); // Залишаємо звичайний StateService
builder.Services.AddAuthenticationStateDeserialization();

await builder.Build().RunAsync();