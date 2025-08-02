namespace DnsClient.IntegratedClients.Dynv6;

public record Dynv6ZoneInfo(
    string Name,
    string Ipv4address,
    string Ipv6prefix,
    int? Id,
    DateTime? CreatedAt,
    DateTime? UpdatedAt
);

