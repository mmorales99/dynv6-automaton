namespace DnsClient.IntegratedClients.Dynv6;

public record Dynv6Record(
    string Name,
    int? Priority,
    int? Port,
    int? Weight,
    int? Flags,
    string Tag,
    string Data,
    string ExpandedData,
    int? Id,
    int? ZoneID,
    string Type
);