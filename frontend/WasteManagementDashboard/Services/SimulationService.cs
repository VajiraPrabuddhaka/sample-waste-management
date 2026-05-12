using System.Net.Http.Json;
using WasteManagementDashboard.Models;

namespace WasteManagementDashboard.Services;

public class SimulationService
{
    private readonly HttpClient _http;
    public SimulationService(HttpClient http) => _http = http;

    public async Task<SimulationStatus?> GetStatusAsync() =>
        await _http.GetFromJsonAsync<SimulationStatus>("api/simulation/status");

    public async Task<SimulationStatus?> ToggleChaosAsync()
    {
        var response = await _http.PutAsync("api/simulation/chaos", null);
        return await response.Content.ReadFromJsonAsync<SimulationStatus>();
    }

    public async Task<SimulationStatus?> ToggleAccelerateAsync()
    {
        var response = await _http.PutAsync("api/simulation/accelerate", null);
        return await response.Content.ReadFromJsonAsync<SimulationStatus>();
    }

    public async Task<HttpResponseMessage> ResetAsync() =>
        await _http.PostAsync("api/simulation/reset", null);
}
