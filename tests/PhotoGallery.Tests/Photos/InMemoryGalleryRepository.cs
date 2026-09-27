using System.Collections.Concurrent;
using Amazon.DynamoDBv2.Model;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;

namespace PhotoGallery.Tests.Photos;

internal sealed class InMemoryGalleryRepository : IGalleryRepository
{
    private readonly ConcurrentDictionary<(string UserId, string AlbumId), Album> _albums = new();
    private readonly ConcurrentDictionary<(string UserId, string AlbumId, string PhotoId), Photo> _photos = new();
    private readonly ConcurrentDictionary<string, Share> _shares = new();

    public Task CreateAlbumAsync(Album album, CancellationToken ct = default)
    {
        if (!_albums.TryAdd((album.UserId, album.AlbumId), album))
            throw new InvalidOperationException("Album already exists.");

        return Task.CompletedTask;
    }

    public Task<Album?> GetAlbumAsync(string userId, string albumId, CancellationToken ct = default) =>
        Task.FromResult(_albums.GetValueOrDefault((userId, albumId)));

    public Task<IReadOnlyList<Album>> ListAlbumsAsync(string userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Album>>(_albums.Values.Where(a => a.UserId == userId).ToList());

    public Task CreatePhotoAsync(Photo photo, CancellationToken ct = default)
    {
        if (!_albums.ContainsKey((photo.UserId, photo.AlbumId)))
            throw new TransactionCanceledException("Album does not exist.");

        _photos[(photo.UserId, photo.AlbumId, photo.PhotoId)] = photo;
        return Task.CompletedTask;
    }

    public Task<Photo?> GetPhotoAsync(string userId, string albumId, string photoId, CancellationToken ct = default) =>
        Task.FromResult(_photos.GetValueOrDefault((userId, albumId, photoId)));

    public Task<IReadOnlyList<Photo>> ListPhotosAsync(string userId, string albumId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Photo>>(
            _photos.Values.Where(p => p.UserId == userId && p.AlbumId == albumId).ToList());

    public Task<bool> MarkPhotoReadyAsync(string userId, string albumId, string photoId, ProcessedImage image, CancellationToken ct = default)
    {
        if (_photos.GetValueOrDefault((userId, albumId, photoId)) is not { Status: PhotoStatus.Pending } photo)
            return Task.FromResult(false);

        _photos[(userId, albumId, photoId)] = photo with
        {
            Status = PhotoStatus.Ready,
            ThumbnailKey = image.ThumbnailKey,
            PreviewKey = image.PreviewKey,
            Width = image.Width,
            Height = image.Height
        };

        var album = _albums[(userId, albumId)];
        _albums[(userId, albumId)] = album with
        {
            PhotoCount = album.PhotoCount + 1,
            CoverPhotoKey = album.CoverPhotoKey ?? image.ThumbnailKey
        };

        return Task.FromResult(true);
    }

    public Task<bool> DeletePhotoAsync(Photo photo, CancellationToken ct = default)
    {
        var key = (photo.UserId, photo.AlbumId, photo.PhotoId);
        if (_photos.GetValueOrDefault(key) is not { } stored || stored.Status != photo.Status)
            return Task.FromResult(false);

        _photos.TryRemove(key, out _);

        if (photo.Status == PhotoStatus.Ready)
        {
            var album = _albums[(photo.UserId, photo.AlbumId)];
            _albums[(photo.UserId, photo.AlbumId)] = album with { PhotoCount = album.PhotoCount - 1 };
        }

        return Task.FromResult(true);
    }

    public Task SetAlbumCoverAsync(string userId, string albumId, string? coverPhotoKey, CancellationToken ct = default)
    {
        var album = _albums[(userId, albumId)];
        _albums[(userId, albumId)] = album with { CoverPhotoKey = coverPhotoKey };
        return Task.CompletedTask;
    }

    public Task CreateShareAsync(Share share, CancellationToken ct = default)
    {
        if (!_albums.ContainsKey((share.OwnerUserId, share.AlbumId)))
            throw new TransactionCanceledException("Album does not exist.");

        _shares[share.Code] = share;
        return Task.CompletedTask;
    }

    public Task<Share?> GetShareAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_shares.GetValueOrDefault(code));

    public Task<bool> RenameAlbumAsync(string userId, string albumId, string name, CancellationToken ct = default)
    {
        if (!_albums.TryGetValue((userId, albumId), out var album))
            return Task.FromResult(false);

        _albums[(userId, albumId)] = album with { Name = name };
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<Photo>?> DeleteAlbumAsync(string userId, string albumId, CancellationToken ct = default)
    {
        if (!_albums.TryRemove((userId, albumId), out _))
            return Task.FromResult<IReadOnlyList<Photo>?>(null);

        var photos = _photos.Values.Where(p => p.UserId == userId && p.AlbumId == albumId).ToList();
        foreach (var photo in photos)
            _photos.TryRemove((userId, albumId, photo.PhotoId), out _);

        return Task.FromResult<IReadOnlyList<Photo>?>(photos);
    }

    public Task<bool> UpdatePhotoDetailsAsync(
        string userId, string albumId, string photoId, string? caption, IReadOnlyList<string> tags, CancellationToken ct = default)
    {
        if (!_photos.TryGetValue((userId, albumId, photoId), out var photo))
            return Task.FromResult(false);

        _photos[(userId, albumId, photoId)] = photo with { Caption = caption, Tags = tags };
        return Task.FromResult(true);
    }
}