namespace Dyndns.Service.Options;

public sealed class SmtpNotificationOptions
{
    public const string SectionName = "Notifications";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string FromAddress { get; set; } = string.Empty;

    public string ToAddress { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string SubjectPrefix { get; set; } = "Dynv6 automaton";
}