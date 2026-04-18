using System.Text.Json.Serialization;

namespace Dyndns.Service.Models;

public sealed record Dynv6RecordUpdateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("priority")]
    public int? Priority { get; init; }

    [JsonPropertyName("port")]
    public int? Port { get; init; }

    [JsonPropertyName("weight")]
    public int? Weight { get; init; }

    [JsonPropertyName("flags")]
    public int? Flags { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "A";
}