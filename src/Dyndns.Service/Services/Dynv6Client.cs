using Dyndns.Service.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Dyndns.Service.Services;


/// <summary>
/// Dynv6 API Consumer.
/// It consumes the dynv6 api folliwing this specification https://dynv6.github.io/api-spec/
/// 
/// It gets the complete list of zones, then filters by name.
/// Then for a named zone, it gets the records and filters the wildcard A record.
/// Then updates the A record value and the zone IPv4 address.
/// </summary>
public sealed class Dynv6Client : IDynv6Client
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Dynv6Client> _logger;
    private readonly IDynv6SettingsService _settingsService;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Dynv6Client(HttpClient httpClient, ILogger<Dynv6Client> logger, IDynv6SettingsService settingsService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<Dynv6Zone> GetZoneByNameAsync(string zoneName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing Dynv6 zones to find '{ZoneName}'.", zoneName);
        var zones = await GetZonesAsync(cancellationToken);
        var zone = zones.FirstOrDefault(candidate => string.Equals(candidate.Name, zoneName, StringComparison.OrdinalIgnoreCase));

        return zone ?? throw new InvalidOperationException($"The zone '{zoneName}' was not found or is not accessible.");
    }

    public async Task<IReadOnlyList<Dynv6Record>> GetRecordsAsync(long zoneId, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, $"zones/{zoneId}/records", cancellationToken);
        _logger.LogDebug("Sending Dynv6 request for records in zone {ZoneId}.", zoneId);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadListAsync<Dynv6Record>(response, $"records for zone {zoneId}");
    }

    public async Task<Dynv6Zone> UpdateZoneAsync(long zoneId, Dynv6ZoneUpdateRequest request, CancellationToken cancellationToken)
    {
        var httpRequest = await CreateRequestAsync(HttpMethod.Patch, $"zones/{zoneId}", cancellationToken);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);
        _logger.LogInformation("Updating Dynv6 zone {ZoneId}.", zoneId);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        return await ReadSingleAsync<Dynv6Zone>(response, $"zone {zoneId}");
    }

    public async Task<Dynv6Record> UpdateRecordAsync(long zoneId, long recordId, Dynv6RecordUpdateRequest request, CancellationToken cancellationToken)
    {
        var httpRequest = await CreateRequestAsync(HttpMethod.Patch, $"zones/{zoneId}/records/{recordId}", cancellationToken);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);
        _logger.LogInformation("Updating Dynv6 record {RecordId} in zone {ZoneId}.", recordId, zoneId);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        return await ReadSingleAsync<Dynv6Record>(response, $"record {recordId}");
    }

    private async Task<IReadOnlyList<Dynv6Zone>> GetZonesAsync(CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, "zones", cancellationToken);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadListAsync<Dynv6Zone>(response, "zones");
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
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Dynv6 {ResourceDescription} returned 404 Not Found.", resourceDescription);
            throw new InvalidOperationException($"The {resourceDescription} was not found or is not accessible.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Dynv6 {ResourceDescription} request failed with {StatusCode} {ReasonPhrase}.",
                resourceDescription,
                (int)response.StatusCode,
                response.ReasonPhrase);
            throw await CreateExceptionAsync(response, resourceDescription);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        return payload ?? throw new InvalidOperationException($"The {resourceDescription} response was empty.");
    }

    private async Task<IReadOnlyList<T>> ReadListAsync<T>(HttpResponseMessage response, string resourceDescription)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Dynv6 {ResourceDescription} returned 404 Not Found.", resourceDescription);
            throw new InvalidOperationException($"The {resourceDescription} was not found or is not accessible.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Dynv6 {ResourceDescription} request failed with {StatusCode} {ReasonPhrase}.",
                resourceDescription,
                (int)response.StatusCode,
                response.ReasonPhrase);
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