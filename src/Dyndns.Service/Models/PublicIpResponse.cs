using System.Text.Json.Serialization;

namespace Dyndns.Service.Models;

public sealed class PublicIpResponse
{
    [JsonPropertyName("ip")]
    public string? Ip { get; init; }
}