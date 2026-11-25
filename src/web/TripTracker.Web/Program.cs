using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TripTracker.Web;
using TripTracker.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Singletons: HttpClientFactory builds message handlers in its own scope, and BearerTokenHandler must
// reach the same session as the UI, or a sign-out caused by a 401 would not update the pages.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITokenStore, LocalStorageTokenStore>();
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(
    services => services.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddSingleton<IAuthSession>(
    services => services.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

var api = ApiSettings.FromConfiguration(builder.Configuration);
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services
    .AddHttpClient<IApiClient, ApiClient>(client => client.BaseAddress = api.BaseUrl)
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
