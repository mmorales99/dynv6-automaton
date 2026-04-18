using System.Text.Json.Serialization;

namespace Dyndns.Service.Models;

public sealed record Dynv6Zone
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("ipv4address")]
    public string? Ipv4Address { get; init; }

    [JsonPropertyName("ipv6prefix")]
    public string? Ipv6Prefix { get; init; }
}