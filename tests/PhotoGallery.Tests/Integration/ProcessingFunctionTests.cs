using Amazon.Lambda.S3Events;
using Amazon.Lambda.TestUtilities;
using Amazon.S3.Model;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Processing;
using PhotoGallery.Tests.Processing;

namespace PhotoGallery.Tests.Integration;

[Trait("Category", "Integration")]
public class ProcessingFunctionTests
{
    private const string SkipReason = "LocalStack is not running on 127.0.0.1:4566";

    private static readonly GalleryOptions Options = LocalStack.Options;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_CreatesWebpVariants_AndMarksPhotoReady()
    {
        Assert.SkipUnless(LocalStack.IsRunning, SkipReason);
        var repository = new DynamoGalleryRepository(AwsClientFactory.CreateDynamoDb(Options), Options);
        var s3 = AwsClientFactory.CreateS3(Options);
        var userId = Keys.NewId();
        var albumId = Keys.NewId();
        var photoId = Keys.NewId();
        var originalKey = S3Keys.Original(userId, albumId, photoId, "photo.jpg");

        await repository.CreateAlbumAsync(new Album
        {
            UserId = userId,
            AlbumId = albumId,
            Name = "Processing",
            CreatedAt = DateTimeOffset.UtcNow
        }, Ct);
        await repository.CreatePhotoAsync(new Photo
        {
            UserId = userId,
            AlbumId = albumId,
            PhotoId = photoId,
            OriginalKey = originalKey,
            CreatedAt = DateTimeOffset.UtcNow
        }, Ct);

        using var jpeg = TestImages.CreateJpeg(1600, 800);
        var size = jpeg.Length;
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Options.BucketName,
            Key = originalKey,
            InputStream = jpeg,
            ContentType = "image/jpeg"
        }, Ct);

        var function = new ProcessingFunction(repository, s3, new ImageProcessor(), Options);
        await function.HandleAsync(S3EventFor(originalKey, size), new TestLambdaContext());

        var photo = await repository.GetPhotoAsync(userId, albumId, photoId, Ct);
        Assert.Equal(PhotoStatus.Ready, photo!.Status);
        Assert.Equal((1600, 800), (photo.Width, photo.Height));

        var thumbnail = await s3.GetObjectMetadataAsync(Options.BucketName, S3Keys.Thumbnail(photoId), Ct);
        Assert.Equal("image/webp", thumbnail.Headers.ContentType);

        var album = await repository.GetAlbumAsync(userId, albumId, Ct);
        Assert.Equal(1, album!.PhotoCount);
    }

    private static S3Event S3EventFor(string key, long size) => new()
    {
        Records =
        [
            new S3Event.S3EventNotificationRecord
            {
                S3 = new S3Event.S3Entity
                {
                    Object = new S3Event.S3ObjectEntity { Key = key, Size = size }
                }
            }
        ]
    };
}