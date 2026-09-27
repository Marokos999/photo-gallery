namespace PhotoGallery.Core.Models;

public sealed record Share
{
    public required string Code { get; init; }
    public required string OwnerUserId { get; init; }
    public required string AlbumId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt is { } expiresAt && expiresAt <= now;
}