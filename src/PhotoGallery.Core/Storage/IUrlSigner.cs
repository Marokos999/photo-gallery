namespace PhotoGallery.Core.Storage;

public interface IUrlSigner
{
    Task<string> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt);

    Task<string> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt);
}