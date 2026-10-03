using Amazon.S3;
using Amazon.S3.Model;

namespace PhotoGallery.Core.Storage;

public sealed class S3UrlSigner(IAmazonS3 presigner, GalleryOptions options, TimeProvider? time = null) : IUrlSigner
{
    public static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromHours(1);

    private readonly TimeProvider _time = time ?? TimeProvider.System;

    private readonly Protocol _protocol =
        options.PresignEndpoint?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true
            ? Protocol.HTTP
            : Protocol.HTTPS;

    /// <summary>
    /// Presigned POST instead of PUT: the policy lets S3 itself reject files outside the size range
    /// and content types other than the one requested. A presigned PUT cannot limit the body size.
    /// </summary>
    public async Task<UploadForm> CreateUploadFormAsync(string key, string contentType, DateTimeOffset expiresAt)
    {
        var response = await presigner.CreatePresignedPostAsync(new CreatePresignedPostRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Expires = expiresAt.UtcDateTime,
            Fields = new Dictionary<string, string> { ["Content-Type"] = contentType },
            Conditions =
            [
                S3PostCondition.ContentLengthRange(1, UploadLimits.MaxFileBytes),
                S3PostCondition.ExactMatch("Content-Type", contentType)
            ]
        });

        return new UploadForm(response.Url, response.Fields);
    }

    public Task<string> CreateDownloadUrlAsync(string key) =>
        presigner.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = _time.GetUtcNow().Add(DownloadUrlLifetime).UtcDateTime,
            Protocol = _protocol
        });
}
