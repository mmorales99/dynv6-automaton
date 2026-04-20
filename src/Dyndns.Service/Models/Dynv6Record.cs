using System.Text.Json.Serialization;

namespace Dyndns.Service.Models;

public sealed record Dynv6Record
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("zoneID")]
    public long ZoneId { get; init; }

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

    [JsonPropertyName("expandedData")]
    public string? ExpandedData { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "A";

    public Dynv6RecordUpdateRequest ToUpdateRequest(string data) => new()
    {
        Name = Name,
        Priority = Priority,
        Port = Port,
        Weight = Weight,
        Flags = Flags,
        Tag = Tag,
        Data = data,
        Type = Type
    };
}