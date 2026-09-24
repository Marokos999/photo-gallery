using Amazon.DynamoDBv2.Model;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;

namespace PhotoGallery.Tests.Integration;

[Trait("Category", "Integration")]
public class DynamoGalleryRepositoryTests
{
    private const string SkipReason = "LocalStack is not running on 127.0.0.1:4566";

    private readonly DynamoGalleryRepository _repository =
        new(AwsClientFactory.CreateDynamoDb(LocalStack.Options), LocalStack.Options);

    private readonly string _userId = Keys.NewId();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateAlbum_CanBeReadAndListed()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Summer");

        await _repository.CreateAlbumAsync(album, Ct);

        Assert.Equivalent(album, await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct));
        Assert.Single(await _repository.ListAlbumsAsync(_userId, Ct));
    }

    [Fact]
    public async Task CreatePhoto_IncrementsAlbumCount_AndIsListedInAlbum()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Trip");
        await _repository.CreateAlbumAsync(album, Ct);

        await _repository.CreatePhotoAsync(NewPhoto(album.AlbumId), Ct);
        await _repository.CreatePhotoAsync(NewPhoto(album.AlbumId), Ct);

        var stored = await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct);
        Assert.Equal(2, stored!.PhotoCount);
        Assert.Equal(2, (await _repository.ListPhotosAsync(_userId, album.AlbumId, Ct)).Count);
        Assert.Empty(await _repository.ListAlbumsAsync(Keys.NewId(), Ct));
    }

    [Fact]
    public async Task CreatePhoto_ForMissingAlbum_Throws()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);

        await Assert.ThrowsAsync<TransactionCanceledException>(
            () => _repository.CreatePhotoAsync(NewPhoto(Keys.NewId()), Ct));
    }

    [Fact]
    public async Task MarkPhotoReady_UpdatesStatusAndKeys()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Ready");
        await _repository.CreateAlbumAsync(album, Ct);
        var photo = NewPhoto(album.AlbumId);
        await _repository.CreatePhotoAsync(photo, Ct);

        var image = new ProcessedImage(S3Keys.Thumbnail(photo.PhotoId), S3Keys.Preview(photo.PhotoId), 4000, 3000);
        await _repository.MarkPhotoReadyAsync(_userId, album.AlbumId, photo.PhotoId, image, Ct);

        var stored = await _repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct);
        Assert.Equal(PhotoStatus.Ready, stored!.Status);
        Assert.Equal(image.ThumbnailKey, stored.ThumbnailKey);
        Assert.Equal(4000, stored.Width);
    }

    private Album NewAlbum(string name) => new()
    {
        UserId = _userId,
        AlbumId = Keys.NewId(),
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private Photo NewPhoto(string albumId)
    {
        var photoId = Keys.NewId();
        return new Photo
        {
            UserId = _userId,
            AlbumId = albumId,
            PhotoId = photoId,
            OriginalKey = S3Keys.Original(_userId, albumId, photoId, "photo.jpg"),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}