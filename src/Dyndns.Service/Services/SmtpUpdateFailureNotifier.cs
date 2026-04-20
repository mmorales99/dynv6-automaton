using Dyndns.Service.Options;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Dyndns.Service.Services;

public sealed class SmtpUpdateFailureNotifier : IUpdateFailureNotifier
{
    private readonly ILogger<SmtpUpdateFailureNotifier> _logger;
    private readonly IOptions<SmtpNotificationOptions> _options;

    public SmtpUpdateFailureNotifier(
        ILogger<SmtpUpdateFailureNotifier> logger,
        IOptions<SmtpNotificationOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task SendAsync(string subject, string body, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = _options.Value;
        ValidateSettings(settings);

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress),
            Subject = $"{settings.SubjectPrefix}: {subject}",
            Body = body
        };

        message.To.Add(settings.ToAddress);

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        }

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send failure notification email.");
            throw new InvalidOperationException("Failed to send Dynv6 failure notification email.", exception);
        }
    }

    private static void ValidateSettings(SmtpNotificationOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            throw new InvalidOperationException("Notifications:SmtpHost is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.FromAddress))
        {
            throw new InvalidOperationException("Notifications:FromAddress is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.ToAddress))
        {
            throw new InvalidOperationException("Notifications:ToAddress is required.");
        }
    }
}