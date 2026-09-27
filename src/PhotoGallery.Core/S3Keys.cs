using System.Diagnostics.CodeAnalysis;

namespace PhotoGallery.Core;

public sealed record OriginalKeyParts(string UserId, string AlbumId, string PhotoId, string FileName);

public static class S3Keys
{
    public const string OriginalsPrefix = "originals/";

    public static string Original(string userId, string albumId, string photoId, string fileName) =>
        $"{OriginalsPrefix}{userId}/{albumId}/{photoId}/{fileName}";

    public static string Thumbnail(string photoId) => $"thumbs/{photoId}_400.webp";

    public static string Preview(string photoId) => $"thumbs/{photoId}_1200.webp";

    public static bool TryParseOriginal(string key, [NotNullWhen(true)] out OriginalKeyParts? parts)
    {
        parts = null;

        if (!key.StartsWith(OriginalsPrefix, StringComparison.Ordinal))
            return false;

        var segments = key[OriginalsPrefix.Length..].Split('/');
        if (segments.Length != 4 || segments.Any(string.IsNullOrWhiteSpace))
            return false;

        parts = new OriginalKeyParts(segments[0], segments[1], segments[2], segments[3]);
        return true;
    }
}