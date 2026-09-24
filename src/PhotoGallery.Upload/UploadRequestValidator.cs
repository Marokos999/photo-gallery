using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Upload;

public sealed record ValidUpload(
    string FileName,
    string ContentType,
    string AlbumId,
    string? Caption,
    IReadOnlyList<string> Tags);

public static partial class UploadRequestValidator
{
    private const int MaxCaptionLength = 500;
    private const int MaxTags = 20;
    private const int MaxTagLength = 50;

    public static bool TryValidate(
        UploadRequest? request,
        [NotNullWhen(true)] out ValidUpload? upload,
        [NotNullWhen(false)] out string? error)
    {
        upload = null;
        error = request switch
        {
            null => "Request body must be a valid JSON object.",
            { FileName: null or "" } => "fileName is required.",
            { AlbumId: var albumId } when albumId is null || !IdFormat().IsMatch(albumId) => "albumId is invalid.",
            { ContentType: var ct } when !ImageContentTypes.IsAllowed(ct) =>
                "contentType must be image/jpeg, image/png or image/webp.",
            { Caption.Length: > MaxCaptionLength } => $"caption must be at most {MaxCaptionLength} characters.",
            { Tags.Count: > MaxTags } => $"At most {MaxTags} tags are allowed.",
            { Tags: { } tags } when tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > MaxTagLength) =>
                $"Each tag must be 1-{MaxTagLength} characters.",
            _ => null
        };

        if (error is not null)
            return false;

        upload = new ValidUpload(
            request!.FileName!,
            request.ContentType!,
            request.AlbumId!,
            request.Caption,
            request.Tags ?? []);
        return true;
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex IdFormat();
}