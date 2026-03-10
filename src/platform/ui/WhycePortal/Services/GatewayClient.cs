namespace Whycespace.Platform.UI.WhycePortal.Services;

using System.Text.Json;

public sealed class GatewayClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GatewayClient(HttpClient http)
    {
        _http = http;
    }

    // --- Queries ---

    public Task<JsonElement> GetDriverLocationsAsync() =>
        GetJsonAsync("api/queries/drivers/locations");

    public Task<JsonElement> GetPropertyListingsAsync() =>
        GetJsonAsync("api/queries/properties/listings");

    public Task<JsonElement> GetVaultBalancesAsync() =>
        GetJsonAsync("api/queries/vaults/balances");

    public Task<JsonElement> GetRevenueAsync() =>
        GetJsonAsync("api/queries/revenue");

    public Task<JsonElement> GetWorkflowsAsync() =>
        GetJsonAsync("api/queries/workflows");

    // --- Operator ---

    public Task<JsonElement> GetClustersAsync() =>
        GetJsonAsync("api/operator/clusters");

    public Task<JsonElement> GetEnginesAsync() =>
        GetJsonAsync("api/operator/engines");

    public Task<JsonElement> GetInvocationsAsync() =>
        GetJsonAsync("api/operator/invocations");

    public Task<JsonElement> GetDeadLettersAsync() =>
        GetJsonAsync("api/operator/deadletters");

    // --- Debug ---

    public Task<JsonElement> GetDevWorkflowsAsync() =>
        GetJsonAsync("dev/workflows");

    public Task<JsonElement> GetDevEnginesAsync() =>
        GetJsonAsync("dev/engines");

    public Task<JsonElement> GetDevProjectionsAsync() =>
        GetJsonAsync("dev/projections");

    public Task<JsonElement> GetEventsAsync() =>
        GetJsonAsync("dev/events");

    // --- Internal ---

    private async Task<JsonElement> GetJsonAsync(string path)
    {
        try
        {
            var response = await _http.GetAsync(path);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            return doc.RootElement.Clone();
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }
}
