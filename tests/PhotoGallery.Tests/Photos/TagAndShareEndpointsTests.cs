using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Photos;

namespace PhotoGallery.Tests.Photos;

public class TagAndShareEndpointsTests(PhotosApiFactory factory) : IClassFixture<PhotosApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _userId = Keys.NewId();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_ReturnsReadyPhotosWithTag_AcrossAlbums_CaseInsensitive()
    {
        var summer = await factory.Repository.SeedAlbumAsync(_userId, "Summer");
        var trips = await factory.Repository.SeedAlbumAsync(_userId, "Trips");
        var first = await factory.Repository.SeedPhotoAsync(_userId, summer.AlbumId, ready: true, tags: ["Sea"]);
        var second = await factory.Repository.SeedPhotoAsync(_userId, trips.AlbumId, ready: true, tags: ["sea", "boat"]);
        await factory.Repository.SeedPhotoAsync(_userId, trips.AlbumId, ready: false, tags: ["sea"]); // still processing
        await factory.Repository.SeedPhotoAsync(_userId, trips.AlbumId, ready: true, tags: ["mountain"]);

        var page = await factory.CreateClientFor(_userId).GetFromJsonAsync<PhotoPageResponse>("/api/photos?tag=SEA", Json, Ct);

        Assert.Equivalent(new[] { first.PhotoId, second.PhotoId }, page!.Items.Select(p => p.PhotoId));
        Assert.Contains(page.Items, p => p.AlbumId == trips.AlbumId);
    }

    [Fact]
    public async Task Search_WithoutTag_Returns400()
    {
        var response = await factory.CreateClientFor(_userId).GetAsync("/api/photos?tag=%20", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tags_AreCounted_MostUsedFirst()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, tags: ["sea", "sun"]);
        await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId, tags: ["Sea"]);

        var tags = await factory.CreateClientFor(_userId).GetFromJsonAsync<List<TagCount>>("/api/tags", Json, Ct);

        Assert.Equal("sea", tags![0].Tag, ignoreCase: true); // "sea" and "Sea" are one tag
        Assert.Equal(2, tags[0].Count);
        Assert.Equal(new TagCount("sun", 1), tags[1]);
    }

    [Fact]
    public async Task Shares_AreListed_AndRevokedLinkStopsWorking()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var client = factory.CreateClientFor(_userId);
        var created = await (await client.PostAsJsonAsync($"/api/albums/{album.AlbumId}/share", new CreateShareRequest(3), Ct))
            .Content.ReadFromJsonAsync<ShareResponse>(Ct);

        var listed = await client.GetFromJsonAsync<List<ShareSummaryResponse>>($"/api/albums/{album.AlbumId}/shares", Ct);
        var revoke = await client.DeleteAsync($"/api/shares/{created!.Code}", Ct);
        var publicView = await factory.CreateClient().GetAsync($"/api/shared/{created.Code}", Ct);

        Assert.Equal(created.Code, Assert.Single(listed!).Code);
        Assert.False(listed![0].IsExpired);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, publicView.StatusCode);
    }

    [Fact]
    public async Task RevokeShare_OfAnotherUser_Returns404_AndKeepsLink()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var share = new Share { Code = Keys.NewShareCode(), OwnerUserId = _userId, AlbumId = album.AlbumId, CreatedAt = DateTimeOffset.UtcNow };
        await factory.Repository.CreateShareAsync(share, Ct);

        var response = await factory.CreateClientFor(Keys.NewId()).DeleteAsync($"/api/shares/{share.Code}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await factory.Repository.GetShareAsync(share.Code, Ct));
    }
}
