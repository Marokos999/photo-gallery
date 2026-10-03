using PhotoGallery.Core.Models;

namespace PhotoGallery.Core.Persistence;

public interface IGalleryRepository
{
    Task CreateAlbumAsync(Album album, CancellationToken ct = default);
    Task<Album?> GetAlbumAsync(string userId, string albumId, CancellationToken ct = default);
    Task<IReadOnlyList<Album>> ListAlbumsAsync(string userId, CancellationToken ct = default);

    Task CreatePhotoAsync(Photo photo, CancellationToken ct = default);
    Task<Photo?> GetPhotoAsync(string userId, string albumId, string photoId, CancellationToken ct = default);
    Task<IReadOnlyList<Photo>> ListPhotosAsync(string userId, string albumId, CancellationToken ct = default);

    /// <summary>Newest first. <paramref name="cursor"/> must come from a previous page of the same album.</summary>
    Task<PhotoPage> ListPhotosPageAsync(string userId, string albumId, int limit, string? cursor, CancellationToken ct = default);
    Task<bool> MarkPhotoReadyAsync(string userId, string albumId, string photoId, ProcessedImage image, CancellationToken ct = default);
    Task<bool> MarkPhotoFailedAsync(string userId, string albumId, string photoId, CancellationToken ct = default);
    Task<bool> DeletePhotoAsync(Photo photo, CancellationToken ct = default);

    Task SetAlbumCoverAsync(string userId, string albumId, string? coverPhotoKey, CancellationToken ct = default);

    Task CreateShareAsync(Share share, CancellationToken ct = default);

    Task<Share?> GetShareAsync(string code, CancellationToken ct = default);

    /// <summary>Share links of an album owned by <paramref name="userId"/>, including expired ones not yet removed by TTL.</summary>
    Task<IReadOnlyList<Share>> ListSharesAsync(string userId, string albumId, CancellationToken ct = default);

    /// <summary>Revokes a share link. Returns false if it does not exist or belongs to someone else.</summary>
    Task<bool> DeleteShareAsync(string userId, string code, CancellationToken ct = default);

    Task<bool> RenameAlbumAsync(string userId, string albumId, string name, CancellationToken ct = default);

    Task<IReadOnlyList<Photo>?> DeleteAlbumAsync(string userId, string albumId, CancellationToken ct = default);

    /// <summary>Photos carrying <paramref name="tag"/> (case-insensitive), newest first, across all albums.</summary>
    Task<PhotoPage> SearchByTagAsync(string userId, string tag, int limit, string? cursor, CancellationToken ct = default);

    /// <summary>All tags of the user with the number of photos carrying each, most used first.</summary>
    Task<IReadOnlyList<TagCount>> ListTagsAsync(string userId, CancellationToken ct = default);

    Task<bool> UpdatePhotoDetailsAsync(
        string userId, string albumId, string photoId, string? caption, IReadOnlyList<string> tags, CancellationToken ct = default);
}