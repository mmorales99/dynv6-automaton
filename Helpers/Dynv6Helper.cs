using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;

namespace MCVIngenieros;

public static class Dynv6Helper 
{
    /// <summary>
    /// Updates the A record in the DNS Zone asociated to a Hostname with the new IP. Needed for Google Indexation, Minecraft Server Check up and other services.
    /// Given the hostname, y querys for Zone Id and Record Id.
    /// Then Updates the value of the record.
    /// </summary>
    /// <param name="HostName">Hostname asociated to the DNS Zone.</param>
    /// <param name="HttpToken">HTTP Token of the owner fo the DNS Zone.</param>
    /// <param name="newIp">New Ip that will be updated in the A record of the DNS Zone.</param>
    /// <exception cref="NullReferenceException">If no information could be retrieved with the Hostname or HTTP Token, this will give an error.</exception>
    public static void UpdateDNSZoneARecord(string HostName, string HttpToken, string newIp)
    {
        HttpRequestMessage request;
        HttpResponseMessage response;
        string apiResource;

        // Get ZoneId by name
        apiResource = $"https://dynv6.com/api/v2/zones/by-name/{HostName}";
        request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(apiResource),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HttpToken);
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json)
        );
        response = Constants.httpClient.Send(request);
        GetZoneInfoByNameResult? getZoneInfoByNameResult = null;
        if (response.IsSuccessStatusCode)
        {
            getZoneInfoByNameResult = response.Content.ReadFromJsonAsync<GetZoneInfoByNameResult>(
                options: Constants.JsonSerializerOptions
            ).Result;
        }

        if (getZoneInfoByNameResult == null)
        {
            throw new NullReferenceException("GetZoneInfoByName resulted in nothing, an error has happend...");
        }

        // Get RecordId by name
        apiResource = $"https://dynv6.com/api/v2/zones/{getZoneInfoByNameResult!.id}/records";
        request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(apiResource),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HttpToken);
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json)
        );
        response = Constants.httpClient.Send(request);
        GetZoneRecordsInfoResult? getZoneRecordsInfoResult = null;
        if (response.IsSuccessStatusCode)
        {
            var listOfRecords = response.Content.ReadFromJsonAsync<List<GetZoneRecordsInfoResult>>(
                options: Constants.JsonSerializerOptions
            ).Result;
            getZoneRecordsInfoResult = listOfRecords?.FirstOrDefault(x => x.type.Equals("A", StringComparison.OrdinalIgnoreCase));
        }

        if (getZoneRecordsInfoResult == null)
        {
            throw new NullReferenceException("GetZoneInfoByName resulted in nothing, an error has happend...");
        }

        // Update A record
        apiResource = $"https://dynv6.com/api/v2/zones/{getZoneInfoByNameResult.id}/records/{getZoneRecordsInfoResult.id}";
        request = new HttpRequestMessage
        {
            Method = HttpMethod.Patch,
            RequestUri = new Uri(apiResource),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", HttpToken);
        request.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json)
        );

        getZoneRecordsInfoResult.data = newIp;
        request.Content = JsonContent.Create(getZoneRecordsInfoResult);

        response = Constants.httpClient.Send(request);
        //if (response.IsSuccessStatusCode)
        //{
        //    // TODO mark run as ok
        //}
    }

    /// <summary>
    /// Updates the asociated IP of a DNS Zone. Makes an HTTP GET to the given url formated with new ip, hostname and http token.
    /// </summary>
    /// <param name="HostName">Domain name to update.</param>
    /// <param name="HttpToken">HTTP Token asociated to owner of DNS Zone.</param>
    /// <param name="LastPublicIpPath">Path to file containing last Ip.</param>
    /// <param name="DyndnsAPIUrl">Url to use for updating the IP.</param>
    /// <param name="newIp">The new Ip to use.</param>
    public static void UpdateDNSZoneIp(
        string HostName,
        string HttpToken,
        string LastPublicIpPath,
        string DyndnsAPIUrl,
        string newIp)
    {
        // Update zone ip
        // "https://ipv4.dynv6.com/api/update?ipv4=$ipv4&zone=$hostname&token=$httpToken"
        string apiResource = DyndnsAPIUrl;
        apiResource = apiResource.Replace("$ipv4", newIp);
        apiResource = apiResource.Replace("$hostname", HostName);
        apiResource = apiResource.Replace("$httpToken", HttpToken);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(apiResource),
        };
        var response = Constants.httpClient.Send(request);
        if (response.IsSuccessStatusCode)
        {
            // TODO: Mark as ok
        }
    }
}