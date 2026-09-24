using System.Globalization;
using System.Text.Json.Nodes;

namespace ZOVserver.AdminPanel.Backend.Services;

public class MetricsAggregatorService(HttpClient httpClient)
{
    private const string QueryPath = "prometheus/api/v1/query?query=";

    public async Task<int> GetSum(string metricName, string tenantId)
    {
        try
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, QueryPath + Uri.EscapeDataString($"sum({metricName})"));
            request.Headers.Add("X-Scope-OrgID", tenantId);

            var responseMessage = await httpClient.SendAsync(request);
            responseMessage.EnsureSuccessStatusCode();

            var response = await responseMessage.Content.ReadFromJsonAsync<JsonObject>();

            var result = response?["data"]?["result"]?.AsArray();

            if (result == null || result.Count == 0)
                return 0;

            var valueStr = result[0]?["value"]?[1]?.GetValue<string>();

            return (int)double.Parse(valueStr ?? "0", CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching metrics: {ex.Message}");
            return 0;
        }
    }
}