namespace PhotoGallery.Core.Storage;

/// <summary>Browser form upload: POST <see cref="Url"/> as multipart/form-data with <see cref="Fields"/> followed by the file.</summary>
public sealed record UploadForm(string Url, IReadOnlyDictionary<string, string> Fields);

public interface IUrlSigner
{
    Task<UploadForm> CreateUploadFormAsync(string key, string contentType, DateTimeOffset expiresAt);

    /// <summary>Presigned GET URL. The signer owns the lifetime; callers get a URL valid for at least 15 minutes.</summary>
    Task<string> CreateDownloadUrlAsync(string key);
}
