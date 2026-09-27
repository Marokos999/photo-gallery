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
    public async Task CreatePhoto_IsListedInAlbum_ButNotCountedUntilReady()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Trip");
        await _repository.CreateAlbumAsync(album, Ct);

        await _repository.CreatePhotoAsync(NewPhoto(album.AlbumId), Ct);
        await _repository.CreatePhotoAsync(NewPhoto(album.AlbumId), Ct);

        var stored = await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct);
        Assert.Equal(0, stored!.PhotoCount);
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
    public async Task MarkPhotoReady_IsIdempotent_AndCountsOnce()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Ready");
        await _repository.CreateAlbumAsync(album, Ct);
        var photo = NewPhoto(album.AlbumId);
        await _repository.CreatePhotoAsync(photo, Ct);
        var image = new ProcessedImage(S3Keys.Thumbnail(photo.PhotoId), S3Keys.Preview(photo.PhotoId), 4000, 3000);

        var first = await _repository.MarkPhotoReadyAsync(_userId, album.AlbumId, photo.PhotoId, image, Ct);
        var second = await _repository.MarkPhotoReadyAsync(_userId, album.AlbumId, photo.PhotoId, image, Ct);

        Assert.True(first);
        Assert.False(second);

        var storedPhoto = await _repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct);
        Assert.Equal(PhotoStatus.Ready, storedPhoto!.Status);
        Assert.Equal(4000, storedPhoto.Width);

        var storedAlbum = await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct);
        Assert.Equal(1, storedAlbum!.PhotoCount);
        Assert.Equal(image.ThumbnailKey, storedAlbum.CoverPhotoKey);
    }
    private async Task<(Album Album, Photo Photo)> CreateReadyPhotoAsync()
    {
        var album = NewAlbum("Album");
        await _repository.CreateAlbumAsync(album, Ct);
        var photo = NewPhoto(album.AlbumId);
        await _repository.CreatePhotoAsync(photo, Ct);
        var image = new ProcessedImage(S3Keys.Thumbnail(photo.PhotoId), S3Keys.Preview(photo.PhotoId), 800, 600);
        await _repository.MarkPhotoReadyAsync(_userId, album.AlbumId, photo.PhotoId, image, Ct);

        var ready = await _repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct);
        return (album, ready!);
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

    [Fact]
    public async Task DeletePhoto_Ready_RemovesItAndDecrementsCount()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var (album, photo) = await CreateReadyPhotoAsync();

        Assert.True(await _repository.DeletePhotoAsync(photo, Ct));

        Assert.Null(await _repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct));
        Assert.Equal(0, (await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct))!.PhotoCount);
    }

    [Fact]
    public async Task DeletePhoto_WithStaleStatus_ReturnsFalse()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var (_, readyPhoto) = await CreateReadyPhotoAsync();
        var stalePendingCopy = readyPhoto with { Status = PhotoStatus.Pending };

        Assert.False(await _repository.DeletePhotoAsync(stalePendingCopy, Ct));
    }

    [Fact]
    public async Task SetAlbumCover_SetsAndRemoves()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Cover");
        await _repository.CreateAlbumAsync(album, Ct);

        await _repository.SetAlbumCoverAsync(_userId, album.AlbumId, "thumbs/x_400.webp", Ct);
        Assert.Equal("thumbs/x_400.webp", (await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct))!.CoverPhotoKey);

        await _repository.SetAlbumCoverAsync(_userId, album.AlbumId, null, Ct);
        Assert.Null((await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct))!.CoverPhotoKey);
    }

    [Fact]
    public async Task CreateShare_CanBeReadByCode()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Shared");
        await _repository.CreateAlbumAsync(album, Ct);
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var share = new Share
        {
            Code = Keys.NewShareCode(),
            OwnerUserId = _userId,
            AlbumId = album.AlbumId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7)
        };

        await _repository.CreateShareAsync(share, Ct);

        Assert.Equivalent(share, await _repository.GetShareAsync(share.Code, Ct));
    }

    [Fact]
    public async Task CreateShare_ForMissingAlbum_Throws()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var share = new Share { Code = Keys.NewShareCode(), OwnerUserId = _userId, AlbumId = Keys.NewId() };

        await Assert.ThrowsAsync<TransactionCanceledException>(() => _repository.CreateShareAsync(share, Ct));
    }

    [Fact]
    public async Task RenameAlbum_UpdatesName_AndReturnsFalseWhenMissing()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Before");
        await _repository.CreateAlbumAsync(album, Ct);

        Assert.True(await _repository.RenameAlbumAsync(_userId, album.AlbumId, "After", Ct));
        Assert.False(await _repository.RenameAlbumAsync(_userId, Keys.NewId(), "After", Ct));
        Assert.Equal("After", (await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct))!.Name);
    }

    [Fact]
    public async Task DeleteAlbum_RemovesAlbumAndAllPhotos()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Doomed");
        await _repository.CreateAlbumAsync(album, Ct);
        for (var i = 0; i < 30; i++)
            await _repository.CreatePhotoAsync(NewPhoto(album.AlbumId), Ct);

        var deleted = await _repository.DeleteAlbumAsync(_userId, album.AlbumId, Ct);

        Assert.Equal(30, deleted!.Count);
        Assert.Null(await _repository.GetAlbumAsync(_userId, album.AlbumId, Ct));
        Assert.Empty(await _repository.ListPhotosAsync(_userId, album.AlbumId, Ct));
    }

    [Fact]
    public async Task UpdatePhotoDetails_SetsAndClearsCaption()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var album = NewAlbum("Details");
        await _repository.CreateAlbumAsync(album, Ct);
        var photo = NewPhoto(album.AlbumId);
        await _repository.CreatePhotoAsync(photo, Ct);

        await _repository.UpdatePhotoDetailsAsync(_userId, album.AlbumId, photo.PhotoId, "Hello", ["a", "b"], Ct);
        var withCaption = await _repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct);
        await _repository.UpdatePhotoDetailsAsync(_userId, album.AlbumId, photo.PhotoId, null, [], Ct);
        var cleared = await _repository.GetPhotoAsync(_userId, album.AlbumId, photo.PhotoId, Ct);

        Assert.Equal("Hello", withCaption!.Caption);
        Assert.Equal(["a", "b"], withCaption.Tags);
        Assert.Null(cleared!.Caption);
        Assert.Empty(cleared.Tags);
    }
}