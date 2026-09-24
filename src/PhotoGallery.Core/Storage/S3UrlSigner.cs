using Amazon.S3;
using Amazon.S3.Model;

namespace PhotoGallery.Core.Storage;

public sealed class S3UrlSigner(IAmazonS3 presigner, GalleryOptions options) : IUrlSigner
{
    private readonly Protocol _protocol =
        options.PresignEndpoint?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true
            ? Protocol.HTTP
            : Protocol.HTTPS;

    public Task<string> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt) =>
        presigner.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAt.UtcDateTime,
            Protocol = _protocol
        });

    public Task<string> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt) =>
        presigner.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime,
            Protocol = _protocol
        });
}