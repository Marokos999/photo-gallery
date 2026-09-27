using System.Net;
using System.Net.Http.Json;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Photos;

namespace PhotoGallery.Tests.Photos;

public class ShareEndpointsTests(PhotosApiFactory factory) : IClassFixture<PhotosApiFactory>
{
    private readonly string _userId = Keys.NewId();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateShare_ThenOpenAnonymously_ShowsOnlyReadyPhotos()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId, "Holiday");
        var ready = await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, ready: true);
        await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId);

        var created = await factory.CreateClientFor(_userId)
            .PostAsJsonAsync($"/api/albums/{album.AlbumId}/share", new CreateShareRequest(3), Ct);
        var share = await created.Content.ReadFromJsonAsync<ShareResponse>(Ct);

        var shared = await factory.CreateClient()
            .GetFromJsonAsync<SharedAlbumResponse>($"/api/shared/{share!.Code}", Ct);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Holiday", shared!.Name);
        Assert.Equal(ready.PhotoId, Assert.Single(shared.Photos).PhotoId);
    }

    [Fact]
    public async Task CreateShare_WithoutBody_DefaultsTo7Days()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);

        var created = await factory.CreateClientFor(_userId)
            .PostAsync($"/api/albums/{album.AlbumId}/share", null, Ct);
        var share = await created.Content.ReadFromJsonAsync<ShareResponse>(Ct);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.InRange(share!.ExpiresAt, DateTimeOffset.UtcNow.AddDays(6.9), DateTimeOffset.UtcNow.AddDays(7.1));
    }

    [Fact]
    public async Task CreateShare_WithTooLongExpiry_Returns400()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);

        var response = await factory.CreateClientFor(_userId)
            .PostAsJsonAsync($"/api/albums/{album.AlbumId}/share", new CreateShareRequest(31), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateShare_ForAnotherUsersAlbum_Returns404()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);

        var response = await factory.CreateClientFor(Keys.NewId())
            .PostAsJsonAsync($"/api/albums/{album.AlbumId}/share", new CreateShareRequest(3), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SharedAlbum_WhenExpired_Returns404()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var share = new Share
        {
            Code = Keys.NewShareCode(),
            OwnerUserId = _userId,
            AlbumId = album.AlbumId,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        await factory.Repository.CreateShareAsync(share, Ct);

        var response = await factory.CreateClient().GetAsync($"/api/shared/{share.Code}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SharedAlbum_WithUnknownCode_Returns404()
    {
        var response = await factory.CreateClient().GetAsync($"/api/shared/{Keys.NewShareCode()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}