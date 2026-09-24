namespace PhotoGallery.Core.Models;

public sealed record Photo
{
    public required string UserId { get; init; }
    public required string AlbumId { get; init; }
    public required string PhotoId { get; init; }
    public required string OriginalKey { get; init; }
    public string? ThumbnailKey { get; init; }
    public string? PreviewKey { get; init; }
    public string? Caption { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public PhotoStatus Status { get; init; } = PhotoStatus.Pending;
    public int? Width { get; init; }
    public int? Height { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}