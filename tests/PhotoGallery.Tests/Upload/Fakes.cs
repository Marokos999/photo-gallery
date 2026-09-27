using Amazon.DynamoDBv2.Model;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Tests.Upload;

internal sealed class FakeGalleryRepository : IGalleryRepository
{
    public bool AlbumExists { get; set; } = true;

    public List<Photo> CreatedPhotos { get; } = [];

    public Task CreatePhotoAsync(Photo photo, CancellationToken ct = default)
    {
        if (!AlbumExists)
            throw new TransactionCanceledException("Album does not exist.");

        CreatedPhotos.Add(photo);
        return Task.CompletedTask;
    }

    public Task CreateAlbumAsync(Album album, CancellationToken ct = default) => throw new NotImplementedException();

    public Task<Album?> GetAlbumAsync(string userId, string albumId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Album>> ListAlbumsAsync(string userId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<Photo?> GetPhotoAsync(string userId, string albumId, string photoId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Photo>> ListPhotosAsync(string userId, string albumId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<bool> MarkPhotoReadyAsync(string userId, string albumId, string photoId, ProcessedImage image, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<bool> MarkPhotoFailedAsync(string userId, string albumId, string photoId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<bool> DeletePhotoAsync(Photo photo, CancellationToken ct = default) => throw new NotImplementedException();

    public Task SetAlbumCoverAsync(string userId, string albumId, string? coverPhotoKey, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task CreateShareAsync(Share share, CancellationToken ct = default) => throw new NotImplementedException();

    public Task<Share?> GetShareAsync(string code, CancellationToken ct = default) => throw new NotImplementedException();

    public Task<bool> RenameAlbumAsync(string userId, string albumId, string name, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Photo>?> DeleteAlbumAsync(string userId, string albumId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<bool> UpdatePhotoDetailsAsync(
        string userId, string albumId, string photoId, string? caption, IReadOnlyList<string> tags, CancellationToken ct = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeUrlSigner : IUrlSigner
{
    public Task<string> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt) =>
        Task.FromResult($"https://signed.test/{key}?contentType={contentType}");

    public Task<string> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt) =>
        Task.FromResult($"https://signed.test/{key}");
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}