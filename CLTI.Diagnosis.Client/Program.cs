using CLTI.Diagnosis.Client.Algoritm.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<StateService>();
builder.Services.AddAuthenticationStateDeserialization();

await builder.Build().RunAsync();