using System.Security.Cryptography;
using Dyndns.Service.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Dyndns.Service.Services;

public sealed class FileBsonUserStore : IUserStore
{
    private const int CurrentHashVersion = 2;
    private const int PasswordIterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private readonly string _usersFilePath;
    private readonly string _passwordPepper;
    private const string HashVersionField = "hashVersion";

    public FileBsonUserStore(string? usersFilePath = null, string? passwordPepper = null)
    {
        _usersFilePath = string.IsNullOrWhiteSpace(usersFilePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Dyndns",
                "users.bson")
            : usersFilePath;
        _passwordPepper = passwordPepper ?? string.Empty;
    }

    public bool IsInitialized => File.Exists(_usersFilePath);
    private bool HasPepper => !string.IsNullOrWhiteSpace(_passwordPepper);

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
            { "version", HasPepper ? CurrentHashVersion : 1 },
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

    public async Task<AuthenticatedUser?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken)
    {
        if (!IsInitialized)
        {
            return null;
        }

        var document = LoadDocument();
        var users = ReadUsers(document);
        var user = users.FirstOrDefault(candidate => string.Equals(candidate.Name, userName, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return null;
        }

        var verified = PasswordHasher.Verify(password, user.PasswordHash, user.Salt, user.Iterations, user.HashVersion, _passwordPepper);
        if (verified && user.HashVersion < CurrentHashVersion && !string.IsNullOrWhiteSpace(_passwordPepper))
        {
            UpgradeUserHash(document, user, password);
            await SaveDocumentAsync(document, cancellationToken);
        }

        return verified ? new AuthenticatedUser(user.Name, user.Roles) : null;
    }

    private BsonDocument CreateUserDocument(string name, string password, string roles)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = PasswordHasher.Hash(password, salt, PasswordIterations, _passwordPepper);

        return new BsonDocument
        {
            { "name", name },
            { "passwordHash", Convert.ToBase64String(hash) },
            { "salt", Convert.ToBase64String(salt) },
            { "iterations", PasswordIterations },
            { "hashVersion", HasPepper ? CurrentHashVersion : 1 },
            { "roles", roles }
        };
    }

    private BsonDocument LoadDocument()
    {
        return BsonSerializer.Deserialize<BsonDocument>(File.ReadAllBytes(_usersFilePath));
    }

    private static IReadOnlyList<UserRecord> ReadUsers(BsonDocument document)
    {
        var users = document["users"].AsBsonArray;

        return users
            .Select(user => user.AsBsonDocument)
            .Select(user => new UserRecord(
                user["name"].AsString,
                user["passwordHash"].AsString,
                user["salt"].AsString,
                user["iterations"].ToInt32(),
                user.Contains(HashVersionField) ? user[HashVersionField].ToInt32() : 1,
                user["roles"].AsString,
                user))
            .ToArray();
    }

    private async Task SaveDocumentAsync(BsonDocument document, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_usersFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(_usersFilePath))
        {
            File.SetAttributes(_usersFilePath, FileAttributes.Normal);
        }

        try
        {
            await File.WriteAllBytesAsync(_usersFilePath, document.ToBson(), cancellationToken);
        }
        finally
        {
            if (File.Exists(_usersFilePath))
            {
                File.SetAttributes(_usersFilePath, File.GetAttributes(_usersFilePath) | FileAttributes.ReadOnly);
            }
        }
    }

    private void UpgradeUserHash(BsonDocument document, UserRecord user, string password)
    {
        var updatedHash = PasswordHasher.Hash(password, Convert.FromBase64String(user.Salt), user.Iterations, _passwordPepper);
        user.Document["passwordHash"] = Convert.ToBase64String(updatedHash);
        user.Document[HashVersionField] = CurrentHashVersion;
        document["version"] = CurrentHashVersion;
    }

    private static void ValidatePassword(string password, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("A password is required.", parameterName);
        }
    }

    private sealed record UserRecord(string Name, string PasswordHash, string Salt, int Iterations, int HashVersion, string Roles, BsonDocument Document);

    private static class PasswordHasher
    {
        public static byte[] Hash(string password, byte[] salt, int iterations, string pepper)
        {
            var secret = string.IsNullOrEmpty(pepper) ? password : password + pepper;
            return Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, HashAlgorithmName.SHA256, HashSize);
        }

        public static bool Verify(string password, string passwordHash, string salt, int iterations, int hashVersion, string pepper)
        {
            var computed = Hash(password, Convert.FromBase64String(salt), iterations, hashVersion >= CurrentHashVersion && !string.IsNullOrWhiteSpace(pepper) ? pepper : string.Empty);
            return CryptographicOperations.FixedTimeEquals(computed, Convert.FromBase64String(passwordHash));
        }
    }
}
