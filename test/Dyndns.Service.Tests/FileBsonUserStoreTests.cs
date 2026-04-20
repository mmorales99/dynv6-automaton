using Dyndns.Service.Services;

namespace Dyndns.Service.Tests;

public class FileBsonUserStoreTests
{
    [Fact]
    public async Task InitializeAsync_CreatesReadonlyUsersFile_AndAuthenticatesBothUsers()
    {
        var usersFilePath = Path.Combine(Path.GetTempPath(), $"dyndns-users-{Guid.NewGuid():N}.bson");

        try
        {
            var store = new FileBsonUserStore(usersFilePath);

            await store.InitializeAsync("admin-secret", "viewer-secret", CancellationToken.None);

            Assert.True(File.Exists(usersFilePath));
            Assert.True((File.GetAttributes(usersFilePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);

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
}
