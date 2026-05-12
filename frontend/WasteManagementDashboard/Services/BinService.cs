using System.Net.Http.Json;
using WasteManagementDashboard.Models;

namespace WasteManagementDashboard.Services;

public class BinService
{
    private readonly HttpClient _http;
    public BinService(HttpClient http) => _http = http;

    public async Task<IList<BinSummary>?> GetBinsAsync(string? type = null, string? status = null)
    {
        var url = "api/bins";
        var query = new List<string>();
        if (!string.IsNullOrEmpty(type)) query.Add($"type={type}");
        if (!string.IsNullOrEmpty(status)) query.Add($"status={status}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        return await _http.GetFromJsonAsync<IList<BinSummary>>(url);
    }

    public async Task<BinDetail?> GetBinDetailAsync(int id) =>
        await _http.GetFromJsonAsync<BinDetail>($"api/bins/{id}");

    public async Task<IList<FillLevelReadingDto>?> GetBinReadingsAsync(int id, int hours = 24) =>
        await _http.GetFromJsonAsync<IList<FillLevelReadingDto>>($"api/bins/{id}/readings?hours={hours}");

    public async Task<HttpResponseMessage> CreateBinAsync(CreateBinRequest req) =>
        await _http.PostAsJsonAsync("api/bins", req);
}
