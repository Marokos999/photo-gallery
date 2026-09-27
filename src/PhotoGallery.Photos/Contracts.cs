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
    PhotoStatus Status,
    string? Caption,
    IReadOnlyList<string> Tags,
    int? Width,
    int? Height,
    DateTimeOffset CreatedAt,
    string? ThumbnailUrl,
    string? PreviewUrl);