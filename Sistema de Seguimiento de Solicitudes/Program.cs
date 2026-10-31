using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sistema_de_Seguimiento_de_Solicitudes;

using Sistema_de_Seguimiento_de_Solicitudes.Services;
using Sistema_de_Seguimiento_de_Solicitudes.Services.LoginServices;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<JwtAuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<JwtAuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();

    return new HttpClient(handler)
    {
        BaseAddress = new Uri("https://localhost:7125/")
    };
});

builder.Services.AddScoped<ICalendarioService, CalendarioService>();

// Registrando los servicios
builder.Services.AddScoped<IExpedienteService, ExpedienteService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Register CustomAuthenticationStateProvider
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());

builder.Services.AddAuthorizationCore();


builder.Services.AddSweetAlert2();

builder.Services.AddRadzenComponents();


await builder.Build().RunAsync();
