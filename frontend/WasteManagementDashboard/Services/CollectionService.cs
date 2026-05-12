using System.Net.Http.Json;
using WasteManagementDashboard.Models;

namespace WasteManagementDashboard.Services;

public class CollectionService
{
    private readonly HttpClient _http;
    public CollectionService(HttpClient http) => _http = http;

    public async Task<IList<CollectionSummary>?> GetCollectionsAsync(string? status = null)
    {
        var url = string.IsNullOrEmpty(status) ? "api/collections" : $"api/collections?status={status}";
        return await _http.GetFromJsonAsync<IList<CollectionSummary>>(url);
    }

    public async Task<HttpResponseMessage> ScheduleCollectionAsync(ScheduleCollectionRequest request) =>
        await _http.PostAsJsonAsync("api/collections", request);

    public async Task<HttpResponseMessage> StartCollectionAsync(int id) =>
        await _http.PutAsync($"api/collections/{id}/start", null);

    public async Task<HttpResponseMessage> CompleteCollectionAsync(int id) =>
        await _http.PutAsJsonAsync($"api/collections/{id}/complete", new { FillLevelAtCollection = (double?)null, Notes = (string?)null });

    public async Task<HttpResponseMessage> CancelCollectionAsync(int id) =>
        await _http.PutAsync($"api/collections/{id}/cancel", null);
}
