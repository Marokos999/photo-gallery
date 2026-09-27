using Amazon.S3;
using Amazon.S3.Model;
using PhotoGallery.Core;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Tests.Integration;

[Trait("Category", "Integration")]
public class S3PhotoStorageTests
{
    private const string SkipReason = "LocalStack is not running on 127.0.0.1:4566";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Delete_RemovesAllGivenObjects()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var s3 = AwsClientFactory.CreateS3(LocalStack.Options);
        var storage = new S3PhotoStorage(s3, LocalStack.Options);
        var photoId = Keys.NewId();
        string[] keys = [S3Keys.Thumbnail(photoId), S3Keys.Preview(photoId)];

        foreach (var key in keys)
            await s3.PutObjectAsync(new PutObjectRequest { BucketName = LocalStack.Options.BucketName, Key = key, ContentBody = "x" }, Ct);

        await storage.DeleteAsync([.. keys, $"thumbs/{photoId}_missing.webp"], Ct);

        foreach (var key in keys)
            await Assert.ThrowsAsync<AmazonS3Exception>(() => s3.GetObjectMetadataAsync(LocalStack.Options.BucketName, key, Ct));
    }
}