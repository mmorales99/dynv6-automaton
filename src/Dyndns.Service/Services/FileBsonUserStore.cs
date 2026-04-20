using System.Security.Cryptography;
using Dyndns.Service.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Dyndns.Service.Services;

public sealed class FileBsonUserStore : IUserStore
{
    private const int PasswordIterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private readonly string _usersFilePath;

    public FileBsonUserStore(string? usersFilePath = null)
    {
        _usersFilePath = string.IsNullOrWhiteSpace(usersFilePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Dyndns",
                "users.bson")
            : usersFilePath;
    }

    public bool IsInitialized => File.Exists(_usersFilePath);

    public async Task InitializeAsync(string adminPassword, string viewerPassword, CancellationToken cancellationToken)
    {
        ValidatePassword(adminPassword, nameof(adminPassword));
        ValidatePassword(viewerPassword, nameof(viewerPassword));

        if (IsInitialized)
        {
            throw new InvalidOperationException("The user store has already been initialized.");
        }

        var document = new BsonDocument
        {
            { "version", 1 },
            { "users", new BsonArray
                {
                    CreateUserDocument("viewer", viewerPassword, "none"),
                    CreateUserDocument("admin", adminPassword, "admin")
                }
            }
        };

        var directoryPath = Path.GetDirectoryName(_usersFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllBytesAsync(_usersFilePath, document.ToBson(), cancellationToken);

        File.SetAttributes(_usersFilePath, File.GetAttributes(_usersFilePath) | FileAttributes.ReadOnly);
    }

    public Task<AuthenticatedUser?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken)
    {
        if (!IsInitialized)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var users = LoadUsers();
        var user = users.FirstOrDefault(candidate => string.Equals(candidate.Name, userName, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        var verified = PasswordHasher.Verify(password, user.PasswordHash, user.Salt, user.Iterations);
        return Task.FromResult(verified ? new AuthenticatedUser(user.Name, user.Roles) : null);
    }

    private static BsonDocument CreateUserDocument(string name, string password, string roles)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = PasswordHasher.Hash(password, salt, PasswordIterations);

        return new BsonDocument
        {
            { "name", name },
            { "passwordHash", Convert.ToBase64String(hash) },
            { "salt", Convert.ToBase64String(salt) },
            { "iterations", PasswordIterations },
            { "roles", roles }
        };
    }

    private IReadOnlyList<UserRecord> LoadUsers()
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(File.ReadAllBytes(_usersFilePath));
        var users = document["users"].AsBsonArray;

        return users
            .Select(user => new UserRecord(
                user["name"].AsString,
                user["passwordHash"].AsString,
                user["salt"].AsString,
                user["iterations"].ToInt32(),
                user["roles"].AsString))
            .ToArray();
    }

    private static void ValidatePassword(string password, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("A password is required.", parameterName);
        }
    }

    private sealed record UserRecord(string Name, string PasswordHash, string Salt, int Iterations, string Roles);

    private static class PasswordHasher
    {
        public static byte[] Hash(string password, byte[] salt, int iterations)
            => Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);

        public static bool Verify(string password, string passwordHash, string salt, int iterations)
        {
            var computed = Hash(password, Convert.FromBase64String(salt), iterations);
            return CryptographicOperations.FixedTimeEquals(computed, Convert.FromBase64String(passwordHash));
        }
    }
}
