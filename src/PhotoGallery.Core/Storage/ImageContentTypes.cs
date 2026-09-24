namespace PhotoGallery.Core.Storage;

public static class ImageContentTypes
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static bool IsAllowed(string? contentType) =>
        contentType is not null && Allowed.Contains(contentType);
}