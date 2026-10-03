using PhotoGallery.Core.Models;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Photos;

public static class ResponseMapping
{
    public static async Task<AlbumResponse> ToResponseAsync(this Album album, IUrlSigner signer) =>
        new(
            album.AlbumId,
            album.Name,
            album.PhotoCount,
            album.CreatedAt,
            await SignOrNullAsync(signer, album.CoverPhotoKey));

    public static async Task<PhotoResponse> ToResponseAsync(this Photo photo, IUrlSigner signer) =>
        new(
            photo.PhotoId,
            photo.AlbumId,
            photo.Status,
            photo.Caption,
            photo.Tags,
            photo.Width,
            photo.Height,
            photo.CreatedAt,
            await SignOrNullAsync(signer, photo.ThumbnailKey),
            await SignOrNullAsync(signer, photo.PreviewKey));

    public static async Task<SharedPhotoResponse> ToSharedResponseAsync(this Photo photo, IUrlSigner signer) =>
        new(
            photo.PhotoId,
            photo.Caption,
            photo.Width,
            photo.Height,
            await SignOrNullAsync(signer, photo.ThumbnailKey),
            await SignOrNullAsync(signer, photo.PreviewKey));

    private static async Task<string?> SignOrNullAsync(IUrlSigner signer, string? key) =>
        key is null ? null : await signer.CreateDownloadUrlAsync(key);
}
