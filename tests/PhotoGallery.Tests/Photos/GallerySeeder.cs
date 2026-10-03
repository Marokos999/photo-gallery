using PhotoGallery.Core;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Tests.Photos;

internal static class GallerySeeder
{
    public static async Task<Album> SeedAlbumAsync(this InMemoryGalleryRepository repository, string userId, string name = "Seeded")
    {
        var album = new Album { UserId = userId, AlbumId = Keys.NewId(), Name = name, CreatedAt = DateTimeOffset.UtcNow };
        await repository.CreateAlbumAsync(album);
        return album;
    }

    public static async Task<Photo> SeedPhotoAsync(
        this InMemoryGalleryRepository repository, string userId, string albumId, bool ready = false, IReadOnlyList<string>? tags = null)
    {
        var photoId = Keys.NewId();
        var photo = new Photo
        {
            UserId = userId,
            AlbumId = albumId,
            PhotoId = photoId,
            OriginalKey = S3Keys.Original(userId, albumId, photoId, "photo.jpg"),
            Tags = tags ?? [],
            CreatedAt = DateTimeOffset.UtcNow
        };
        await repository.CreatePhotoAsync(photo);

        if (!ready)
            return photo;

        var image = new ProcessedImage(S3Keys.Thumbnail(photoId), S3Keys.Preview(photoId), 800, 600);
        await repository.MarkPhotoReadyAsync(userId, albumId, photoId, image);
        return (await repository.GetPhotoAsync(userId, albumId, photoId))!;
    }
}