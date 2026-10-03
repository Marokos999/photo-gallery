using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhotoGallery.Core;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Photos;

namespace PhotoGallery.Tests.Photos;

public class PhotoPaginationTests(PhotosApiFactory factory) : IClassFixture<PhotosApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _userId = Keys.NewId();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Pages_CoverEveryPhotoOnce_NewestFirst()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var seeded = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            seeded.Add((await factory.Repository.SeedPhotoAsync(_userId, album.AlbumId)).PhotoId);
            await Task.Delay(2, Ct); // distinct UUIDv7 milliseconds
        }

        var client = factory.CreateClientFor(_userId);
        var received = new List<string>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var url = $"/api/albums/{album.AlbumId}/photos?limit=2" + (cursor is null ? "" : $"&cursor={cursor}");
            var page = await client.GetFromJsonAsync<PhotoPageResponse>(url, Json, Ct);
            received.AddRange(page!.Items.Select(p => p.PhotoId));
            cursor = page.NextCursor;
            pages++;
        } while (cursor is not null);

        Assert.Equal(3, pages);
        Assert.Equal(Enumerable.Reverse(seeded), received);
    }

    [Fact]
    public async Task Cursor_FromAnotherAlbum_Returns400()
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);
        var foreignCursor = PageCursor.Encode(Keys.PhotoSk(Keys.NewId(), Keys.NewId()));

        var response = await factory.CreateClientFor(_userId)
            .GetAsync($"/api/albums/{album.AlbumId}/photos?cursor={foreignCursor}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Limit_OutOfRange_Returns400(int limit)
    {
        var album = await factory.Repository.SeedAlbumAsync(_userId);

        var response = await factory.CreateClientFor(_userId)
            .GetAsync($"/api/albums/{album.AlbumId}/photos?limit={limit}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PageCursor_RoundTrips_AndRejectsGarbage()
    {
        var sortKey = Keys.PhotoSk("a1", "p1");

        Assert.True(PageCursor.TryDecode(PageCursor.Encode(sortKey), Keys.PhotosInAlbumSkPrefix("a1"), out var decoded));
        Assert.Equal(sortKey, decoded);
        Assert.False(PageCursor.TryDecode("not*base64!", Keys.PhotosInAlbumSkPrefix("a1"), out _));
    }
}
