namespace DnsClient.IntegratedClients.Dynv6;

public class Dynv6Options
{
    public string? Zone { get; set; }
    public string? Token { get; set; }

    public ZoneTokenTuple[]? Tuples { get; set; }

    public class ZoneTokenTuple 
    {
        public string? Zone { get; set; }
        public string? Token { get; set; }
    }
}
