using System.Net;
using PhotoGallery.Core;

namespace PhotoGallery.Tests.Photos;

public class PhotoEndpointsTests(PhotosApiFactory factory) : IClassFixture<PhotosApiFactory>
{
    private readonly string _userId = Keys.NewId();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DeletePhoto_RemovesRecordAndObjects_AndDecrementsCount()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var photo = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, ready: true);

        var response = await factory.CreateClientFor(_userId)
            .DeleteAsync($"/api/albums/{album.AlbumId}/photos/{photo.PhotoId}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await factory.Repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct));
        Assert.Equal(0, (await factory.Repository.GetAlbumAsync(_userId, album.AlbumId, Ct))!.PhotoCount);
        Assert.Contains(photo.OriginalKey, factory.Storage.DeletedKeys);
        Assert.Contains(S3Keys.Preview(photo.PhotoId), factory.Storage.DeletedKeys);
    }

    [Fact]
    public async Task DeletePhoto_WhenItIsTheCover_PromotesAnotherReadyPhoto()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var cover = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, ready: true);
        var other = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, ready: true);

        await factory.CreateClientFor(_userId).DeleteAsync($"/api/albums/{album.AlbumId}/photos/{cover.PhotoId}", Ct);

        var stored = await factory.Repository.GetAlbumAsync(_userId, album.AlbumId, Ct);
        Assert.Equal(other.ThumbnailKey, stored!.CoverPhotoKey);
    }

    [Fact]
    public async Task DeletePhoto_ThatDoesNotExist_Returns404()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);

        var response = await factory.CreateClientFor(_userId)
            .DeleteAsync($"/api/albums/{album.AlbumId}/photos/{Keys.NewId()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}