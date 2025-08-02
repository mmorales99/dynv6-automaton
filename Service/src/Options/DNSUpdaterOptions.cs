namespace Service.src.Options;

public record DNSUpdaterOptions
{
    public bool ForceUpdate = false;
    public string DnsClient = "dynv6";
}
