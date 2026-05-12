using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WasteManagementDashboard;
using WasteManagementDashboard.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient — uses the app's own origin so requests go through the nginx proxy
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register API services
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<BinService>();
builder.Services.AddScoped<TruckService>();
builder.Services.AddScoped<CollectionService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<SimulationService>();

await builder.Build().RunAsync();
