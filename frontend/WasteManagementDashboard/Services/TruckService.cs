using System.Net.Http.Json;
using WasteManagementDashboard.Models;

namespace WasteManagementDashboard.Services;

public class TruckService
{
    private readonly HttpClient _http;
    public TruckService(HttpClient http) => _http = http;

    public async Task<IList<TruckSummary>?> GetTrucksAsync(string? status = null)
    {
        var url = string.IsNullOrEmpty(status) ? "api/trucks" : $"api/trucks?status={status}";
        return await _http.GetFromJsonAsync<IList<TruckSummary>>(url);
    }

    public async Task<TruckDetail?> GetTruckDetailAsync(int id) =>
        await _http.GetFromJsonAsync<TruckDetail>($"api/trucks/{id}");
}
