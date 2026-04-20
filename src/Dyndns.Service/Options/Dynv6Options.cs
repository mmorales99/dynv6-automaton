namespace Dyndns.Service.Options;

public sealed class Dynv6Options
{
    public const string SectionName = "Dynv6";

    public string ZoneName { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public bool ForceUpdate { get; set; }

    public string LastPublicIpPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Dyndns",
        "last-public-ip.txt");

    public string RunHistoryPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Dyndns",
        "run-history.jsonl");

    public string RuntimeSettingsPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Dyndns",
        "dynv6-runtime-settings.json");

    public TimeSpan ScheduledEvery { get; set; } = TimeSpan.FromMinutes(5);

    public string PublicIpUrl { get; set; } = "https://api.ipify.org?format=json";

    public string DyndnsApiUrl { get; set; } = "https://dynv6.com/api/v2/";
}