namespace Helpers.Options;

public record IpInfoOptions
{
    public string LastPublicIpPath = ".lastIp";
    public List<string> PublicIpProviders = [
        "https://ipv4ip.com/?format=json",
        "https://ipinfo.io/json",
        "https://api.ipify.org?format=json",
    ];
}
