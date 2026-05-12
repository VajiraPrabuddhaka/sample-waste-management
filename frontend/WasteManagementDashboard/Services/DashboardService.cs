using System.Net.Http.Json;
using WasteManagementDashboard.Models;

namespace WasteManagementDashboard.Services;

public class DashboardService
{
    private readonly HttpClient _http;
    public DashboardService(HttpClient http) => _http = http;

    public async Task<DashboardSummary?> GetDashboardAsync() =>
        await _http.GetFromJsonAsync<DashboardSummary>("api/dashboard");
}
