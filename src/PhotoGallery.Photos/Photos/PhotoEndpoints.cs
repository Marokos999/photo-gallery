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
    private const int DefaultPageSize = 60;
    private const int MaxPageSize = 100;

    public static RouteGroupBuilder MapPhotoEndpoints(this RouteGroupBuilder api)
    {
        api.MapDelete("/albums/{albumId}/photos/{photoId}", DeletePhoto);
        api.MapPatch("/albums/{albumId}/photos/{photoId}", UpdatePhoto);
        api.MapGet("/photos", SearchByTag);
        api.MapGet("/tags", ListTags);
        return api;
    }

    private static async Task<Results<Ok<PhotoPageResponse>, ValidationProblem>> SearchByTag(
        string? tag,
        int? limit,
        string? cursor,
        ClaimsPrincipal user,
        IGalleryRepository repository,
        IUrlSigner signer,
        CancellationToken ct)
    {
        var normalized = tag?.Trim();
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(normalized) || normalized.Length > PhotoDetailsRules.MaxTagLength)
            errors["tag"] = [$"Must be 1-{PhotoDetailsRules.MaxTagLength} characters."];
        else if (cursor is not null && !PageCursor.TryDecode(cursor, Keys.PhotosWithTagSkPrefix(normalized), out _))
            errors["cursor"] = ["Invalid cursor."];
        if (limit is < 1 or > MaxPageSize)
            errors["limit"] = [$"Must be between 1 and {MaxPageSize}."];
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var page = await repository.SearchByTagAsync(user.GetUserId(), normalized!, limit ?? DefaultPageSize, cursor, ct);

        // Search is a browsing view: only photos that can actually be shown.
        var items = new List<PhotoResponse>();
        foreach (var photo in page.Items.Where(p => p.Status == PhotoStatus.Ready))
            items.Add(await photo.ToResponseAsync(signer));

        return TypedResults.Ok(new PhotoPageResponse(items, page.NextCursor));
    }

    private static async Task<Ok<IReadOnlyList<TagCount>>> ListTags(
        ClaimsPrincipal user,
        IGalleryRepository repository,
        CancellationToken ct) =>
        TypedResults.Ok(await repository.ListTagsAsync(user.GetUserId(), ct));

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdatePhoto(
        string albumId,
        string photoId,
        UpdatePhotoRequest request,
        ClaimsPrincipal user,
        IGalleryRepository repository,
        CancellationToken ct)
    {
        var details = PhotoDetailsRules.Normalize(request.Caption, request.Tags);
        var errors = PhotoDetailsRules.Validate(details);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        if (!IdFormats.IsEntityId(albumId) || !IdFormats.IsEntityId(photoId))
            return TypedResults.NotFound();

        var updated = await repository.UpdatePhotoDetailsAsync(
            user.GetUserId(), albumId, photoId, details.Caption, details.Tags, ct);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
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