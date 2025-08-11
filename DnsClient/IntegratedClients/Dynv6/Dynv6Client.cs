using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json;

namespace DnsClient.IntegratedClients.Dynv6;

public class Dynv6Client(IOptions<Dynv6Options> options) : IDnsClient
{
    private readonly Dynv6Options _options = options.Value;
    private static readonly HttpClientHandler HttpClientHandler = new()
    {
        CheckCertificateRevocationList = false,
        ClientCertificateOptions = ClientCertificateOption.Manual,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        SslProtocols = SslProtocols.None
    };

    private static readonly HttpClient HttpClient = new(HttpClientHandler)
    {
        BaseAddress = new Uri("https://dynv6.com/api/v2/"),
    };
    public void UpdateARecords(string ip) 
    {
        if (string.IsNullOrEmpty(ip)) return;
        if (string.IsNullOrEmpty(_options.Zone)) 
        {
            if (_options.Tuples != null && _options.Tuples.Length > 0) 
            {
                foreach (var tuple in _options.Tuples)
                {
                    if (!string.IsNullOrWhiteSpace(tuple.Zone))
                    {
                        var token = tuple.Token ?? _options.Token;
                        if (string.IsNullOrWhiteSpace(token))
                        {
                            throw new ArgumentException("Token must be provided in Dynv6Options or Tuples.");
                        }
                        UpdateARecords(ip, tuple.Zone, token);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Zone must be provided in Dynv6Options or Tuples must be defined.");
            }
        }
        else
        {
            var token = _options.Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token must be provided in Dynv6Options.");
            }
            UpdateARecords(ip, _options.Zone, token);
        }
    }

    private static void UpdateARecords(string ip, string zone, string token)
    {
        if (string.IsNullOrWhiteSpace(zone) || string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Zone and Token must be provided in Dynv6Options.");
        }

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var zoneId = GetZoneId(zone);
        UpdateZone(ip, zoneId);
        var record = GetARecordId(zoneId);
        if (record != null)
        {
            UpdateARecord(new Dynv6Record(
                record.Name,
                record.Priority,
                record.Port,
                record.Weight,
                record.Flags,
                record.Tag,
                ip, // Update the IP address
                record.ExpandedData,
                record.Id,
                record.ZoneID,
                record.Type
            ));
        }
        else
        {
            CreateARecord(ip, zoneId);
        }
    }

    private static Dynv6Record? GetARecordId(string zoneId)
    {
        var getARecordsUri = $"zones/{zoneId}/records";
        var response = HttpClient.GetAsync(getARecordsUri).Result;
        response.EnsureSuccessStatusCode();
        var responseContent = response.Content.ReadAsStringAsync().Result;
        var records = JsonSerializer.Deserialize<List<Dynv6Record>>(responseContent, Helpers.Constants.JsonSerializerOptions);
        return records?.FirstOrDefault(r => r.Type == "A");
    }

    private static void UpdateARecord(Dynv6Record record)
    {
        var updateRecordUri = $"zones/{record.ZoneID}/records/{record.Id}";
        var request = new HttpRequestMessage(HttpMethod.Patch, updateRecordUri)
        {
            Content = JsonContent.Create(record)
        };
        var response = HttpClient.SendAsync(request).Result;
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = response.Content.ReadAsStringAsync().Result;
            throw new HttpRequestException($"Failed to update A record: {response.StatusCode} - {errorContent}");
        }
    }

    private static void CreateARecord(string ip, string zoneId)
    {
        //var createRecordUri = $"zones/{zoneId}/records";
        //var request = new HttpRequestMessage(HttpMethod.Post, createRecordUri)
        //{
        //    Content = JsonContent.Create(new
        //    {
        //        type = "A",
        //        name = _options.Zone,
        //        ipv4address = ip
        //    })
        //};
        //var response = HttpClient.SendAsync(request).Result;
        //if (!response.IsSuccessStatusCode)
        //{
        //    var errorContent = response.Content.ReadAsStringAsync().Result;
        //    throw new HttpRequestException($"Failed to create A record: {response.StatusCode} - {errorContent}");
        //}
    }

    private static void UpdateZone(string ip, string zoneId)
    {
        var updateZoneUri = $"zones/{zoneId}";
        var request = new HttpRequestMessage(HttpMethod.Patch, updateZoneUri)
        {
            Content = JsonContent.Create(new { ipv4address = ip, ipv6address = "auto" })
        };
        var response = HttpClient.SendAsync(request).Result;
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = response.Content.ReadAsStringAsync().Result;
            throw new HttpRequestException($"Failed to update A records: {response.StatusCode} - {errorContent}");
        }
    }

    private static string GetZoneId(string zoneName)
    {
        var getZoneDetailsByNameUri = $"zones/by-name/{zoneName}";
        var response = HttpClient.GetAsync(getZoneDetailsByNameUri).Result;
        response.EnsureSuccessStatusCode();
        var responseContent = response.Content.ReadAsStringAsync().Result;
        var responseObject = JsonSerializer.Deserialize<Dynv6ZoneInfo>(responseContent, Helpers.Constants.JsonSerializerOptions);
        return responseObject?.Id.ToString() ?? string.Empty;
    }
}