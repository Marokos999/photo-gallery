using System.Diagnostics.CodeAnalysis;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Upload;

public sealed record ValidUpload(
    string FileName,
    string ContentType,
    string AlbumId,
    string? Caption,
    IReadOnlyList<string> Tags);

public static class UploadRequestValidator
{
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
            { AlbumId: var albumId } when !IdFormats.IsEntityId(albumId) => "albumId is invalid.",
            { ContentType: var ct } when !ImageContentTypes.IsAllowed(ct) =>
                "contentType must be image/jpeg, image/png or image/webp.",
            _ => null
        };

        if (error is not null)
            return false;

        // Same caption/tag rules as editing a photo later in the Photos API.
        var details = PhotoDetailsRules.Normalize(request!.Caption, request.Tags);
        if (PhotoDetailsRules.Validate(details) is { Count: > 0 } errors)
        {
            error = errors.Values.First()[0];
            return false;
        }

        upload = new ValidUpload(request.FileName!, request.ContentType!, request.AlbumId!, details.Caption, details.Tags);
        return true;
    }
}
