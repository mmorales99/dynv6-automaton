using Dyndns.Service.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dyndns.Service.Services;

public sealed class Dynv6Client : IDynv6Client
{
    private readonly HttpClient _httpClient;
    private readonly IDynv6SettingsService _settingsService;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Dynv6Client(HttpClient httpClient, IDynv6SettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<Dynv6Zone> GetZoneByNameAsync(string zoneName, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, $"zones/by-name/{Uri.EscapeDataString(zoneName)}", cancellationToken);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadSingleAsync<Dynv6Zone>(response, $"zone '{zoneName}'");
    }

    public async Task<IReadOnlyList<Dynv6Record>> GetRecordsAsync(long zoneId, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, $"zones/{zoneId}/records", cancellationToken);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadListAsync<Dynv6Record>(response, $"records for zone {zoneId}");
    }

    public async Task<Dynv6Record> UpdateRecordAsync(long zoneId, long recordId, Dynv6RecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var httpRequest = await CreateRequestAsync(HttpMethod.Patch, $"zones/{zoneId}/records/{recordId}", cancellationToken);
        httpRequest.Content = JsonContent.Create(request);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        return await ReadSingleAsync<Dynv6Record>(response, $"record {recordId}");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string relativePath, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        var baseUri = new Uri(settings.DyndnsApiUrl, UriKind.Absolute);
        var request = new HttpRequestMessage(method, new Uri(baseUri, relativePath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.Key);
        return request;
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