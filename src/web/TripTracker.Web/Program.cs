using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TripTracker.Web;
using TripTracker.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var api = ApiSettings.FromConfiguration(builder.Configuration);
builder.Services.AddHttpClient<IApiClient, ApiClient>(client => client.BaseAddress = api.BaseUrl);
builder.Services.AddMudServices();

await builder.Build().RunAsync();
