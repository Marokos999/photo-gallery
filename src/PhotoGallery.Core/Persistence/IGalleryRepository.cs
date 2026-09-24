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
    Task<bool> MarkPhotoReadyAsync(string userId, string albumId, string photoId, ProcessedImage image, CancellationToken ct = default);

}