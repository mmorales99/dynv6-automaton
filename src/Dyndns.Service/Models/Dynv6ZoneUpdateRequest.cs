using System.Text.Json.Serialization;

namespace Dyndns.Service.Models;

public sealed record Dynv6ZoneUpdateRequest
{
    [JsonPropertyName("ipv4address")]
    public string? Ipv4Address { get; init; }

    [JsonPropertyName("ipv6prefix")]
    public string? Ipv6Prefix { get; init; }
}