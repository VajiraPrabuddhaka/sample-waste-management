using System.Net.Http.Json;
using WasteManagementDashboard.Models;

namespace WasteManagementDashboard.Services;

public class AlertService
{
    private readonly HttpClient _http;
    public AlertService(HttpClient http) => _http = http;

    public async Task<IList<AlertSummary>?> GetAlertsAsync(bool? acknowledged = null, string? severity = null)
    {
        var url = "api/alerts";
        var query = new List<string>();
        if (acknowledged.HasValue) query.Add($"acknowledged={acknowledged.Value.ToString().ToLower()}");
        if (!string.IsNullOrEmpty(severity)) query.Add($"severity={severity}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await _http.GetFromJsonAsync<IList<AlertSummary>>(url);
    }

    public async Task<HttpResponseMessage> AcknowledgeAlertAsync(int id, string acknowledgedBy) =>
        await _http.PutAsJsonAsync($"api/alerts/{id}/acknowledge", new { AcknowledgedBy = acknowledgedBy });

    public async Task<HttpResponseMessage> AcknowledgeAllAlertsAsync(string acknowledgedBy) =>
        await _http.PutAsJsonAsync("api/alerts/acknowledge-all", new { AcknowledgedBy = acknowledgedBy });
}
