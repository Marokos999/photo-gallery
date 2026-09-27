using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;
using PhotoGallery.Photos.Auth;

namespace PhotoGallery.Photos.Albums;

public static class AlbumEndpoints
{
    private const int MaxNameLength = 100;

    private static readonly TimeSpan UrlLifetime = TimeSpan.FromHours(1);

    public static RouteGroupBuilder MapAlbumEndpoints(this RouteGroupBuilder api)
    {
        var albums = api.MapGroup("/albums");

        albums.MapGet("/", ListAlbums);
        albums.MapPost("/", CreateAlbum);
        albums.MapGet("/{albumId}/photos", ListPhotos);
        albums.MapGet("/{albumId}", GetAlbum);
        albums.MapPatch("/{albumId}", RenameAlbum);
        albums.MapDelete("/{albumId}", DeleteAlbum);

        return api;
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> RenameAlbum(
        string albumId,
        RenameAlbumRequest request,
        ClaimsPrincipal user,
        IGalleryRepository repository,
        CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [$"Name must be 1-{MaxNameLength} characters."]
            });
        }

        if (!IdFormats.IsEntityId(albumId) || !await repository.RenameAlbumAsync(user.GetUserId(), albumId, name, ct))
            return TypedResults.NotFound();

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAlbum(
        string albumId,
        ClaimsPrincipal user,
        IGalleryRepository repository,
        IPhotoStorage storage,
        CancellationToken ct)
    {
        if (!IdFormats.IsEntityId(albumId))
            return TypedResults.NotFound();

        var deletedPhotos = await repository.DeleteAlbumAsync(user.GetUserId(), albumId, ct);
        if (deletedPhotos is null)
            return TypedResults.NotFound();

        var objectKeys = deletedPhotos
            .SelectMany(photo => new[] { photo.OriginalKey, S3Keys.Thumbnail(photo.PhotoId), S3Keys.Preview(photo.PhotoId) })
            .ToList();
        await storage.DeleteAsync(objectKeys, ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<AlbumResponse>, NotFound>> GetAlbum(
      string albumId,
      ClaimsPrincipal user,
      IGalleryRepository repo,
      IUrlSigner signer,
      TimeProvider time,
      CancellationToken ct
    )
    {
        if (!IdFormats.IsEntityId(albumId)) return TypedResults.NotFound();

        var album = await repo.GetAlbumAsync(user.GetUserId(), albumId, ct);

        return album is null
               ? TypedResults.NotFound()
               : TypedResults.Ok(await album.ToResponseAsync(signer, time.GetUtcNow().Add(UrlLifetime)));
    }

    private static async Task<Results<Ok<List<PhotoResponse>>, NotFound>> ListPhotos(
            string albumId,
            ClaimsPrincipal user,
            IGalleryRepository repository,
            IUrlSigner signer,
            TimeProvider time,
            CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (!IdFormats.IsEntityId(albumId) || await repository.GetAlbumAsync(userId, albumId, ct) is null)
            return TypedResults.NotFound();

        var photos = await repository.ListPhotosAsync(userId, albumId, ct);
        var expiresAt = time.GetUtcNow().Add(UrlLifetime);

        var response = new List<PhotoResponse>(photos.Count);
        foreach (var photo in photos.OrderByDescending(p => p.CreatedAt))
            response.Add(await photo.ToResponseAsync(signer, expiresAt));

        return TypedResults.Ok(response);
    }


    private static async Task<Results<Created<AlbumResponse>, ValidationProblem>> CreateAlbum(
      CreateAlbumRequest request,
      ClaimsPrincipal user,
      IGalleryRepository repo,
      TimeProvider time,
      CancellationToken ct
    )
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [$"Name must be 1-{MaxNameLength} characters."]
            });
        }

        var now = time.GetUtcNow();
        var album = new Album
        {
            UserId = user.GetUserId(),
            AlbumId = Keys.NewId(now),
            Name = name,
            CreatedAt = now
        };

        await repo.CreateAlbumAsync(album, ct);

        return TypedResults.Created(
                $"/api/albums/{album.AlbumId}",
                new AlbumResponse(album.AlbumId, album.Name, 0, album.CreatedAt, null));


    }


    private static async Task<Ok<List<AlbumResponse>>> ListAlbums(ClaimsPrincipal user, IGalleryRepository repo,
     IUrlSigner signer, TimeProvider time, CancellationToken ct)
    {
        var albums = await repo.ListAlbumsAsync(user.GetUserId(), ct);
        var expiresAt = time.GetUtcNow().Add(UrlLifetime);
        var response = new List<AlbumResponse>(albums.Count);

        foreach (var album in albums.OrderByDescending(a => a.CreatedAt))
            response.Add(await album.ToResponseAsync(signer, expiresAt));

        return TypedResults.Ok(response);
    }
}