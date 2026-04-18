namespace Dyndns.Service.Options;

public sealed class Dynv6Options
{
    public const string SectionName = "Dynv6";

    public string ZoneName { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string RecordName { get; set; } = "*";

    public string EnvironmentKey { get; set; } = "mcvingenieros";

    public bool ForceUpdate { get; set; }

    public string LastPublicIpPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Dyndns",
        "last-public-ip.txt");

    public string PublicIpUrl { get; set; } = "https://api.ipify.org?format=json";

    public string DyndnsApiUrl { get; set; } = "https://dynv6.com/api/";
}