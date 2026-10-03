namespace PhotoGallery.Upload;

public sealed record UploadRequest(
    string? FileName,
    string? ContentType,
    string? AlbumId,
    string? Caption,
    IReadOnlyList<string>? Tags);

public sealed record UploadResponse(
    string PhotoId,
    string Key,
    string UploadUrl,
    IReadOnlyDictionary<string, string> UploadFields,
    DateTimeOffset ExpiresAt);

public sealed record ErrorResponse(string Error);