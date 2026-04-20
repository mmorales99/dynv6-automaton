using Dyndns.Service.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Dyndns.Service.Tests;

public class FileBsonUserStoreTests
{
    [Fact]
    public async Task InitializeAsync_CreatesReadonlyUsersFile_AndAuthenticatesBothUsersWithPepper()
    {
        var usersFilePath = Path.Combine(Path.GetTempPath(), $"dyndns-users-{Guid.NewGuid():N}.bson");
        var pepper = "pepper-secret";

        try
        {
            var store = new FileBsonUserStore(usersFilePath, pepper);

            await store.InitializeAsync("admin-secret", "viewer-secret", CancellationToken.None);

            Assert.True(File.Exists(usersFilePath));
            Assert.True((File.GetAttributes(usersFilePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);

            var document = BsonSerializer.Deserialize<BsonDocument>(await File.ReadAllBytesAsync(usersFilePath, CancellationToken.None));
            Assert.Equal(2, document["version"].ToInt32());
            Assert.All(document["users"].AsBsonArray, user => Assert.Equal(2, user.AsBsonDocument["hashVersion"].ToInt32()));

            var viewer = await store.AuthenticateAsync("viewer", "viewer-secret", CancellationToken.None);
            var admin = await store.AuthenticateAsync("admin", "admin-secret", CancellationToken.None);

            Assert.NotNull(viewer);
            Assert.NotNull(admin);
            Assert.Equal("viewer", viewer!.Name);
            Assert.Equal("admin", admin!.Name);
            Assert.False(viewer.IsAdmin);
            Assert.True(admin.IsAdmin);
            Assert.Null(await store.AuthenticateAsync("viewer", "wrong", CancellationToken.None));
        }
        finally
        {
            if (File.Exists(usersFilePath))
            {
                File.SetAttributes(usersFilePath, FileAttributes.Normal);
                File.Delete(usersFilePath);
            }
        }
    }

    [Fact]
    public async Task AuthenticateAsync_MigratesLegacyUserToPepperedHash()
    {
        var usersFilePath = Path.Combine(Path.GetTempPath(), $"dyndns-users-{Guid.NewGuid():N}.bson");
        var legacyStore = new FileBsonUserStore(usersFilePath);
        var pepperedStore = new FileBsonUserStore(usersFilePath, "pepper-secret");

        try
        {
            await legacyStore.InitializeAsync("admin-secret", "viewer-secret", CancellationToken.None);

            var viewer = await pepperedStore.AuthenticateAsync("viewer", "viewer-secret", CancellationToken.None);

            Assert.NotNull(viewer);

            var document = BsonSerializer.Deserialize<BsonDocument>(await File.ReadAllBytesAsync(usersFilePath, CancellationToken.None));
            var users = document["users"].AsBsonArray;
            var viewerDocument = users.First(user => user.AsBsonDocument["name"].AsString == "viewer").AsBsonDocument;
            var adminDocument = users.First(user => user.AsBsonDocument["name"].AsString == "admin").AsBsonDocument;

            Assert.Equal(2, viewerDocument["hashVersion"].ToInt32());
            Assert.Equal(1, adminDocument.Contains("hashVersion") ? adminDocument["hashVersion"].ToInt32() : 1);
        }
        finally
        {
            if (File.Exists(usersFilePath))
            {
                File.SetAttributes(usersFilePath, FileAttributes.Normal);
                File.Delete(usersFilePath);
            }
        }
    }
}
