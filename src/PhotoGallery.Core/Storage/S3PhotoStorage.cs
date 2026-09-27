using Amazon.S3;
using Amazon.S3.Model;

namespace PhotoGallery.Core.Storage;

public sealed class S3PhotoStorage(IAmazonS3 s3, GalleryOptions options) : IPhotoStorage
{
    public async Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct = default)
    {
        if (keys.Count == 0)
            return;

        await s3.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = options.BucketName,
            Objects = keys.Select(key => new KeyVersion { Key = key }).ToList(),
            Quiet = true
        }, ct);
    }
}