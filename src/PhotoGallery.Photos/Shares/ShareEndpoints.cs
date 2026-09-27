using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;
using PhotoGallery.Photos.Auth;

namespace PhotoGallery.Photos.Shares;

public static class ShareEndpoints
{
    private const int DefaultExpiryDays = 7;
    private const int MaxExpiryDays = 30;
    private static readonly TimeSpan UrlLifetime = TimeSpan.FromHours(1);

    public static RouteGroupBuilder MapShareEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/albums/{albumId}/share", CreateShare);
        api.MapGet("/shared/{code}", GetSharedAlbum).AllowAnonymous();
        return api;
    }

    private static async Task<Results<Created<ShareResponse>, NotFound, ValidationProblem>> CreateShare(
      string albumId,
      CreateShareRequest? request,
      ClaimsPrincipal user,
      IGalleryRepository repo,
      TimeProvider time,
      CancellationToken ct
    )
    {
        var days = request?.ExpiresInDays ?? DefaultExpiryDays;
        if (days is < 1 or > MaxExpiryDays)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["expiresInDays"] = [$"Must be between 1 and {MaxExpiryDays}."]
            });
        }

        var userId = user.GetUserId();
        if (!IdFormats.IsEntityId(albumId) || await repo.GetAlbumAsync(userId, albumId, ct) is null)
            return TypedResults.NotFound();

        var now = time.GetUtcNow();
        var expiresAt = now.AddDays(days);
        var share = new Share
        {
            Code = Keys.NewShareCode(),
            OwnerUserId = userId,
            AlbumId = albumId,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await repo.CreateShareAsync(share, ct);

        return TypedResults.Created($"/api/shared/{share.Code}", new ShareResponse(share.Code, expiresAt));
    }


    private static async Task<Results<Ok<SharedAlbumResponse>, NotFound>> GetSharedAlbum(
      string code,
      IGalleryRepository repo, IUrlSigner signer,
      TimeProvider time,
      CancellationToken ct
    )
    {
        if (!IdFormats.IsShareCode(code)) return TypedResults.NotFound();

        var now = time.GetUtcNow();
        var share = await repo.GetShareAsync(code, ct);
        if (share is null || share.IsExpired(now)) return TypedResults.NotFound();

        var album = await repo.GetAlbumAsync(share.OwnerUserId, share.AlbumId, ct);
        if (album is null) return TypedResults.NotFound();

        var photos = await repo.ListPhotosAsync(share.OwnerUserId, share.AlbumId, ct);
        var urlsExpireAt = now.Add(UrlLifetime);

        var response = new List<SharedPhotoResponse>();
        foreach (var photo in photos.Where(p => p.Status == PhotoStatus.Ready).OrderByDescending(p => p.CreatedAt))
            response.Add(await photo.ToSharedResponseAsync(signer, urlsExpireAt));

        return TypedResults.Ok(new SharedAlbumResponse(album.Name, share.ExpiresAt, response));
    }
}