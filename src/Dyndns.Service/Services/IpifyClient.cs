using Dyndns.Service.Models;
using System.Net;
using System.Net.Sockets;
using System.Net.Http.Json;

namespace Dyndns.Service.Services;

public sealed class IpifyClient
{
    private readonly HttpClient _httpClient;
    private readonly IDynv6SettingsService _settingsService;

    public IpifyClient(HttpClient httpClient, IDynv6SettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> GetCurrentIpv4AddressAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        var response = await _httpClient.GetFromJsonAsync<PublicIpResponse>(settings.PublicIpUrl, cancellationToken);
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