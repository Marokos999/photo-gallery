using Amazon.S3;
using Amazon.S3.Model;

namespace PhotoGallery.Core.Storage;

public sealed class S3PhotoStorage(IAmazonS3 s3, GalleryOptions options) : IPhotoStorage
{
    private const int DeleteObjectsLimit = 1000;

    public async Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct = default)
    {
        foreach (var chunk in keys.Chunk(DeleteObjectsLimit))
        {
            await s3.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = options.BucketName,
                Objects = chunk.Select(key => new KeyVersion { Key = key }).ToList(),
                Quiet = true
            }, ct);
        }
    }
}