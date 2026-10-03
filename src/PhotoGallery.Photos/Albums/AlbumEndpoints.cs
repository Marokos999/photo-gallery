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
    private const int DefaultPageSize = 60;
    private const int MaxPageSize = 100;


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
      CancellationToken ct
    )
    {
        if (!IdFormats.IsEntityId(albumId)) return TypedResults.NotFound();

        var album = await repo.GetAlbumAsync(user.GetUserId(), albumId, ct);

        return album is null
               ? TypedResults.NotFound()
               : TypedResults.Ok(await album.ToResponseAsync(signer));
    }

    private static async Task<Results<Ok<PhotoPageResponse>, NotFound, ValidationProblem>> ListPhotos(
        string albumId,
        int? limit,
        string? cursor,
        ClaimsPrincipal user,
        IGalleryRepository repository,
        IUrlSigner signer,
        CancellationToken ct)
    {
        if (limit is < 1 or > MaxPageSize)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["limit"] = [$"Must be between 1 and {MaxPageSize}."]
            });
        }

        if (cursor is not null && !PageCursor.TryDecode(cursor, Keys.PhotosInAlbumSkPrefix(albumId), out _))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cursor"] = ["Invalid cursor."]
            });
        }

        var userId = user.GetUserId();
        if (!IdFormats.IsEntityId(albumId) || await repository.GetAlbumAsync(userId, albumId, ct) is null)
            return TypedResults.NotFound();

        var page = await repository.ListPhotosPageAsync(userId, albumId, limit ?? DefaultPageSize, cursor, ct);

        var items = new List<PhotoResponse>(page.Items.Count);
        foreach (var photo in page.Items)
            items.Add(await photo.ToResponseAsync(signer));

        return TypedResults.Ok(new PhotoPageResponse(items, page.NextCursor));
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
     IUrlSigner signer, CancellationToken ct)
    {
        var albums = await repo.ListAlbumsAsync(user.GetUserId(), ct);
        var response = new List<AlbumResponse>(albums.Count);

        foreach (var album in albums.OrderByDescending(a => a.CreatedAt))
            response.Add(await album.ToResponseAsync(signer));

        return TypedResults.Ok(response);
    }
}