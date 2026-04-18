using Dyndns.Service.Options;
using Microsoft.Extensions.Options;

namespace Dyndns.Service.Services;

public sealed class FileUpdateStateStore : IUpdateStateStore
{
    private readonly string _filePath;

    public FileUpdateStateStore(IOptions<Dynv6Options> options)
    {
        _filePath = options.Value.LastPublicIpPath;
    }

    public async Task<string?> ReadLastKnownIpAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        var contents = await File.ReadAllTextAsync(_filePath, cancellationToken);
        var value = contents.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public async Task SaveLastKnownIpAsync(string ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("A valid IP address is required.", nameof(ipAddress));
        }

        var directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(_filePath, ipAddress.Trim(), cancellationToken);
    }
}