namespace Dyndns.Service.Services;

public sealed class FileUpdateStateStore : IUpdateStateStore
{
    private readonly IDynv6SettingsService _settingsService;

    public FileUpdateStateStore(IDynv6SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<string?> ReadLastKnownIpAsync(CancellationToken cancellationToken)
    {
        var filePath = (await _settingsService.GetAsync(cancellationToken)).LastPublicIpPath;

        if (!File.Exists(filePath))
        {
            return null;
        }

        var contents = await File.ReadAllTextAsync(filePath, cancellationToken);
        var value = contents.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public async Task SaveLastKnownIpAsync(string ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("A valid IP address is required.", nameof(ipAddress));
        }

        var filePath = (await _settingsService.GetAsync(cancellationToken)).LastPublicIpPath;
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(filePath, ipAddress.Trim(), cancellationToken);
    }
}