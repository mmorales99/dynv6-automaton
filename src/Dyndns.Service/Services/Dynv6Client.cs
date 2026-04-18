using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dyndns.Service.Services;

public sealed class Dynv6Client : IDynv6Client
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Dynv6Client(HttpClient httpClient, IOptions<Dynv6Options> options)
    {
        var settings = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(settings.DyndnsApiUrl, UriKind.Absolute);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Key);
    }

    public async Task<Dynv6Zone> GetZoneByNameAsync(string zoneName, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"zones/by-name/{Uri.EscapeDataString(zoneName)}", cancellationToken);
        return await ReadSingleAsync<Dynv6Zone>(response, $"zone '{zoneName}'");
    }

    public async Task<IReadOnlyList<Dynv6Record>> GetRecordsAsync(long zoneId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"zones/{zoneId}/records", cancellationToken);
        return await ReadListAsync<Dynv6Record>(response, $"records for zone {zoneId}");
    }

    public async Task<Dynv6Record> UpdateRecordAsync(long zoneId, long recordId, Dynv6RecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PatchAsJsonAsync($"zones/{zoneId}/records/{recordId}", request, cancellationToken);
        return await ReadSingleAsync<Dynv6Record>(response, $"record {recordId}");
    }

    private async Task<T> ReadSingleAsync<T>(HttpResponseMessage response, string resourceDescription)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, resourceDescription);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        return payload ?? throw new InvalidOperationException($"The {resourceDescription} response was empty.");
    }

    private async Task<IReadOnlyList<T>> ReadListAsync<T>(HttpResponseMessage response, string resourceDescription)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, resourceDescription);
        }

        var payload = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
        return payload ?? throw new InvalidOperationException($"The {resourceDescription} response was empty.");
    }

    private static async Task<Exception> CreateExceptionAsync(HttpResponseMessage response, string resourceDescription)
    {
        var body = await response.Content.ReadAsStringAsync();
        return new InvalidOperationException(
            $"The {resourceDescription} request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }
}