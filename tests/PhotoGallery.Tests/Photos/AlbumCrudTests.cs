using System.Net;
using System.Net.Http.Json;
using PhotoGallery.Core;
using PhotoGallery.Photos;

namespace PhotoGallery.Tests.Photos;

public class AlbumCrudTests(PhotosApiFactory factory) : IClassFixture<PhotosApiFactory>
{
    private readonly string _userId = Keys.NewId();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RenameAlbum_UpdatesName()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId, "Old");

        var response = await factory.CreateClientFor(_userId)
            .PatchAsJsonAsync($"/api/albums/{album.AlbumId}", new RenameAlbumRequest("  New name "), Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("New name", (await factory.Repository.GetAlbumAsync(_userId, album.AlbumId, Ct))!.Name);
    }

    [Fact]
    public async Task RenameAlbum_WithEmptyName_Returns400()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);

        var response = await factory.CreateClientFor(_userId)
            .PatchAsJsonAsync($"/api/albums/{album.AlbumId}", new RenameAlbumRequest(" "), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_RemovesAlbumPhotosAndObjects()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var ready = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, ready: true);
        var pending = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId);

        var response = await factory.CreateClientFor(_userId).DeleteAsync($"/api/albums/{album.AlbumId}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await factory.Repository.GetAlbumAsync(_userId, album.AlbumId, Ct));
        Assert.Empty(await factory.Repository.ListPhotosAsync(_userId, album.AlbumId, Ct));
        Assert.Contains(ready.OriginalKey, factory.Storage.DeletedKeys);
        Assert.Contains(S3Keys.Thumbnail(pending.PhotoId), factory.Storage.DeletedKeys);
    }

    [Fact]
    public async Task DeleteAlbum_OfAnotherUser_Returns404()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);

        var response = await factory.CreateClientFor(Keys.NewId()).DeleteAsync($"/api/albums/{album.AlbumId}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await factory.Repository.GetAlbumAsync(_userId, album.AlbumId, Ct));
    }

    [Fact]
    public async Task UpdatePhoto_NormalizesAndStoresCaptionAndTags()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var photo = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, ready: true);

        var response = await factory.CreateClientFor(_userId).PatchAsJsonAsync(
            $"/api/albums/{album.AlbumId}/photos/{photo.PhotoId}",
            new UpdatePhotoRequest(" Sunset ", ["sea", " Sea ", "", "beach"]),
            Ct);

        var stored = await factory.Repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Sunset", stored!.Caption);
        Assert.Equal(["sea", "beach"], stored.Tags);
    }

    [Fact]
    public async Task UpdatePhoto_WithTooManyTags_Returns400()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var photo = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId);
        var tags = Enumerable.Range(0, 21).Select(i => $"tag{i}").ToList();

        var response = await factory.CreateClientFor(_userId).PatchAsJsonAsync(
            $"/api/albums/{album.AlbumId}/photos/{photo.PhotoId}", new UpdatePhotoRequest(null, tags), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
