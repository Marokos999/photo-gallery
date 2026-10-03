using PhotoGallery.Core.Models;

namespace PhotoGallery.Photos;

public sealed record CreateAlbumRequest(string? Name);

public sealed record AlbumResponse(
    string AlbumId,
    string Name,
    int PhotoCount,
    DateTimeOffset CreatedAt,
    string? CoverUrl);

public sealed record PhotoResponse(
    string PhotoId,
    string AlbumId,
    PhotoStatus Status,
    string? Caption,
    IReadOnlyList<string> Tags,
    int? Width,
    int? Height,
    DateTimeOffset CreatedAt,
    string? ThumbnailUrl,
    string? PreviewUrl);
public sealed record CreateShareRequest(int? ExpiresInDays);

public sealed record ShareResponse(string Code, DateTimeOffset ExpiresAt);

public sealed record SharedAlbumResponse(
    string Name,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<SharedPhotoResponse> Photos);

public sealed record SharedPhotoResponse(
    string PhotoId,
    string? Caption,
    int? Width,
    int? Height,
    string? ThumbnailUrl,
    string? PreviewUrl);
public sealed record RenameAlbumRequest(string? Name);

public sealed record UpdatePhotoRequest(string? Caption, IReadOnlyList<string>? Tags);

public sealed record PhotoPageResponse(IReadOnlyList<PhotoResponse> Items, string? NextCursor);

public sealed record ShareSummaryResponse(string Code, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, bool IsExpired);

public sealed record HealthResponse(string Status);
