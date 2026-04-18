using Dyndns.Service.Models;
using Dyndns.Service.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Net.Http.Json;

namespace Dyndns.Service.Services;

public sealed class IpifyClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<Dynv6Options> _options;

    public IpifyClient(HttpClient httpClient, IOptions<Dynv6Options> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<string> GetCurrentIpv4AddressAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<PublicIpResponse>(_options.Value.PublicIpUrl, cancellationToken);
        var ipAddress = response?.Ip;

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new InvalidOperationException("The ipify response was empty.");
        }

        if (!IPAddress.TryParse(ipAddress, out var parsedAddress) || parsedAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException($"The ipify response was not an IPv4 address: {ipAddress}");
        }

        return parsedAddress.ToString();
    }
}