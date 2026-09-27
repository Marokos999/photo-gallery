using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Photos;

namespace PhotoGallery.Tests.Photos;

public class AlbumEndpointsTests(PhotosApiFactory factory) : IClassFixture<PhotosApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _userId = Keys.NewId();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_IsAnonymous()
    {
        var response = await factory.CreateClient().GetAsync("/api/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Albums_WithoutUser_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/albums", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAlbum_ThenList_ReturnsIt()
    {
        var client = factory.CreateClientFor(_userId);

        var created = await client.PostAsJsonAsync("/api/albums", new CreateAlbumRequest("  Summer  "), Ct);
        var album = await created.Content.ReadFromJsonAsync<AlbumResponse>(Json, Ct);
        var list = await client.GetFromJsonAsync<List<AlbumResponse>>("/api/albums", Json, Ct);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal($"/api/albums/{album!.AlbumId}", created.Headers.Location?.ToString());
        Assert.Equal("Summer", album.Name);
        Assert.Equal(album.AlbumId, Assert.Single(list!).AlbumId);
    }

    [Fact]
    public async Task CreateAlbum_WithEmptyName_Returns400()
    {
        var response = await factory.CreateClientFor(_userId)
            .PostAsJsonAsync("/api/albums", new CreateAlbumRequest("   "), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListPhotos_OfAnotherUsersAlbum_Returns404()
    {
        var album = await SeedAlbumAsync();

        var response = await factory.CreateClientFor(Keys.NewId()).GetAsync($"/api/albums/{album.AlbumId}/photos", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListPhotos_ReturnsUrlsOnlyForReadyPhotos()
    {
        var album = await SeedAlbumAsync();
        var pending = await SeedPhotoAsync(album.AlbumId);
        var ready = await SeedPhotoAsync(album.AlbumId);
        await factory.Repository.MarkPhotoReadyAsync(_userId, album.AlbumId, ready.PhotoId,
            new ProcessedImage(S3Keys.Thumbnail(ready.PhotoId), S3Keys.Preview(ready.PhotoId), 800, 600), Ct);

        var photos = await factory.CreateClientFor(_userId)
            .GetFromJsonAsync<List<PhotoResponse>>($"/api/albums/{album.AlbumId}/photos", Json, Ct);

        var readyResponse = Assert.Single(photos!, p => p.PhotoId == ready.PhotoId);
        var pendingResponse = Assert.Single(photos!, p => p.PhotoId == pending.PhotoId);
        Assert.Equal(PhotoStatus.Ready, readyResponse.Status);
        Assert.Contains(S3Keys.Thumbnail(ready.PhotoId), readyResponse.ThumbnailUrl);
        Assert.Null(pendingResponse.ThumbnailUrl);
    }

    private async Task<Album> SeedAlbumAsync()
    {
        var album = new Album { UserId = _userId, AlbumId = Keys.NewId(), Name = "Seeded", CreatedAt = DateTimeOffset.UtcNow };
        await factory.Repository.CreateAlbumAsync(album, Ct);
        return album;
    }

    private async Task<Photo> SeedPhotoAsync(string albumId)
    {
        var photoId = Keys.NewId();
        var photo = new Photo
        {
            UserId = _userId,
            AlbumId = albumId,
            PhotoId = photoId,
            OriginalKey = S3Keys.Original(_userId, albumId, photoId, "photo.jpg"),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await factory.Repository.CreatePhotoAsync(photo, Ct);
        return photo;
    }
}