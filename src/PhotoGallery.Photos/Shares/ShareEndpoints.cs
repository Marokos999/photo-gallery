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

    public static RouteGroupBuilder MapShareEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/albums/{albumId}/share", CreateShare);
        api.MapGet("/shared/{code}", GetSharedAlbum).AllowAnonymous();
        api.MapGet("/albums/{albumId}/shares", ListShares);
        api.MapDelete("/shares/{code}", RevokeShare);
        return api;
    }

    private static async Task<Results<Ok<List<ShareSummaryResponse>>, NotFound>> ListShares(
        string albumId,
        ClaimsPrincipal user,
        IGalleryRepository repository,
        TimeProvider time,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (!IdFormats.IsEntityId(albumId) || await repository.GetAlbumAsync(userId, albumId, ct) is null)
            return TypedResults.NotFound();

        var now = time.GetUtcNow();
        var shares = await repository.ListSharesAsync(userId, albumId, ct);

        return TypedResults.Ok(shares
            .OrderByDescending(share => share.CreatedAt)
            .Select(share => new ShareSummaryResponse(share.Code, share.CreatedAt, share.ExpiresAt, share.IsExpired(now)))
            .ToList());
    }

    private static async Task<Results<NoContent, NotFound>> RevokeShare(
        string code,
        ClaimsPrincipal user,
        IGalleryRepository repository,
        CancellationToken ct)
    {
        // Someone else's code and an unknown code look the same: 404.
        if (!IdFormats.IsShareCode(code) || !await repository.DeleteShareAsync(user.GetUserId(), code, ct))
            return TypedResults.NotFound();

        return TypedResults.NoContent();
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

        var response = new List<SharedPhotoResponse>();
        foreach (var photo in photos.Where(p => p.Status == PhotoStatus.Ready).OrderByDescending(p => p.CreatedAt))
            response.Add(await photo.ToSharedResponseAsync(signer));

        return TypedResults.Ok(new SharedAlbumResponse(album.Name, share.ExpiresAt, response));
    }
}