using PhotoGallery.Core.Models;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Photos;

public static class ResponseMapping
{
    public static async Task<AlbumResponse> ToResponseAsync(this Album album, IUrlSigner signer, DateTimeOffset expireAt)
    => new(
      album.AlbumId,
      album.Name,
      album.PhotoCount,
      album.CreatedAt,
      await SignOrNullAsync(signer, album.CoverPhotoKey, expireAt)
    );

    public static async Task<PhotoResponse> ToResponseAsync(this Photo photo, IUrlSigner signer, DateTimeOffset expiresAt) =>
          new(
              photo.PhotoId,
              photo.Status,
              photo.Caption,
              photo.Tags,
              photo.Width,
              photo.Height,
              photo.CreatedAt,
              await SignOrNullAsync(signer, photo.ThumbnailKey, expiresAt),
              await SignOrNullAsync(signer, photo.PreviewKey, expiresAt));
    public static async Task<SharedPhotoResponse> ToSharedResponseAsync(this Photo photo, IUrlSigner signer, DateTimeOffset expiresAt) =>
        new(
            photo.PhotoId,
            photo.Caption,
            photo.Width,
            photo.Height,
            await SignOrNullAsync(signer, photo.ThumbnailKey, expiresAt),
            await SignOrNullAsync(signer, photo.PreviewKey, expiresAt));
    private static async Task<string?> SignOrNullAsync(IUrlSigner signer, string? key, DateTimeOffset expireAt)
    => key is null ? null : await signer.CreateDownloadUrlAsync(key, expireAt);

}