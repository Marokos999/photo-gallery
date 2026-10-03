namespace PhotoGallery.Core.Models;

/// <summary>Normalized caption and tags, ready to store.</summary>
public sealed record PhotoDetails(string? Caption, IReadOnlyList<string> Tags);

/// <summary>
/// Single source of truth for caption/tag rules, shared by the upload Lambda and the Photos API.
/// Input is normalized first (trimmed, empty tags dropped, case-insensitive duplicates removed), then validated.
/// </summary>
public static class PhotoDetailsRules
{
    public const int MaxCaptionLength = 500;
    public const int MaxTags = 20;
    public const int MaxTagLength = 50;

    public static PhotoDetails Normalize(string? caption, IEnumerable<string?>? tags) =>
        new(
            string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            (tags ?? [])
                .Select(tag => tag?.Trim() ?? "")
                .Where(tag => tag.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());

    /// <summary>Returns field → error message; empty when the details are valid.</summary>
    public static Dictionary<string, string[]> Validate(PhotoDetails details)
    {
        var errors = new Dictionary<string, string[]>();

        if (details.Caption is { Length: > MaxCaptionLength })
            errors["caption"] = [$"Caption must be at most {MaxCaptionLength} characters."];

        if (details.Tags.Count > MaxTags || details.Tags.Any(tag => tag.Length > MaxTagLength))
            errors["tags"] = [$"At most {MaxTags} tags, each up to {MaxTagLength} characters."];

        return errors;
    }
}
