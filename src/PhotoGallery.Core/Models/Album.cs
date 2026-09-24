namespace PhotoGallery.Core.Models;

public sealed record Album
{
    public required string UserId { get; init; }
    public required string AlbumId { get; init; }
    public required string Name { get; init; }
    public string? CoverPhotoKey { get; init; }
    public int PhotoCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}