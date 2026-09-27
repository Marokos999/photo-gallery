using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;
using PhotoGallery.Photos.Auth;

namespace PhotoGallery.Photos.Photos;

public static class PhotoEndpoints
{
    public static RouteGroupBuilder MapPhotoEndpoints(this RouteGroupBuilder api)
    {
        api.MapDelete("/albums/{albumId}/photos/{photoId}", DeletePhoto);
        return api;
    }

    private static async Task<Results<NoContent, NotFound, Conflict>> DeletePhoto(
      string albumId,
      string photoId,
      ClaimsPrincipal user,
      IGalleryRepository repo,
      IPhotoStorage storage,
      CancellationToken ct
    )
    {
        if (!IdFormats.IsEntityId(albumId) || !IdFormats.IsEntityId(photoId)) return TypedResults.NotFound();

        var photo = await repo.GetPhotoAsync(user.GetUserId(), albumId, photoId, ct);
        if (photo is null) return TypedResults.NotFound();

        if (!await repo.DeletePhotoAsync(photo, ct)) return TypedResults.Conflict();

        await storage.DeleteAsync([photo.OriginalKey, S3Keys.Thumbnail(photo.PhotoId), S3Keys.Preview(photo.PhotoId)], ct);
        await ReplaceCoverIfNeededAsync(photo, repo, ct);

        return TypedResults.NoContent();
    }

    private static async Task ReplaceCoverIfNeededAsync(Photo deleted, IGalleryRepository repo, CancellationToken ct)
    {
        var album = await repo.GetAlbumAsync(deleted.UserId, deleted.AlbumId, ct);
        if (deleted.ThumbnailKey is null || album?.CoverPhotoKey != deleted.ThumbnailKey) return;

        var photos = await repo.ListPhotosAsync(deleted.UserId, deleted.AlbumId, ct);

        var nextCover = photos.Where(p => p.Status == PhotoStatus.Ready)
                              .OrderByDescending(o => o.CreatedAt)
                              .FirstOrDefault();

        await repo.SetAlbumCoverAsync(deleted.UserId, deleted.AlbumId, nextCover?.ThumbnailKey, ct);
    }
}