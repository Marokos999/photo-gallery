using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.S3.Model;
using PhotoGallery.Core;
using PhotoGallery.Core.Persistence;
using SixLabors.ImageSharp;
using PhotoGallery.Core.Models;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace PhotoGallery.Processing;

public sealed class ProcessingFunction
{
    private const long MaxOriginalBytes = 25 * 1024 * 1024;

    private readonly IGalleryRepository repo;
    private readonly IAmazonS3 s3;
    private readonly ImageProcessor processor;
    private readonly GalleryOptions options;

    public ProcessingFunction() : this(GalleryOptions.FromEnvironment())
    {

    }

    private ProcessingFunction(GalleryOptions options) : this(new DynamoGalleryRepository(AwsClientFactory.CreateDynamoDb(options), options),
      AwsClientFactory.CreateS3(options), new ImageProcessor(), options)
    {

    }

    public ProcessingFunction(IGalleryRepository repo, IAmazonS3 s3, ImageProcessor processor, GalleryOptions options)
    {
        this.repo = repo;
        this.s3 = s3;
        this.processor = processor;
        this.options = options;
    }

    public async Task HandleAsync(S3Event s3Event, ILambdaContext context)
    {
        foreach (var record in s3Event.Records ?? [])
        {
            await ProcessObjectAsync(record.S3.Object, context.Logger);
        }

    }

    private async Task ProcessObjectAsync(S3Event.S3ObjectEntity s3Object, ILambdaLogger logger)
    {
        var key = s3Object.KeyDecoded;

        if (!S3Keys.TryParseOriginal(key, out var parts))
        {
            logger.LogWarning("Ignoring object with unexpected key {Key}", key);
            return;
        }

        if (s3Object.Size > MaxOriginalBytes)
        {
            logger.LogWarning("Photo {PhotoId} is too large ({Size} bytes)", parts.PhotoId, s3Object.Size);
            return;
        }
        var existing = await repo.GetPhotoAsync(parts.UserId, parts.AlbumId, parts.PhotoId);
        if (existing is not { Status: PhotoStatus.Pending })
        {
            logger.LogInformation("Photo {PhotoId} is not pending, skipping", parts.PhotoId);
            return;
        }


        ProcessedVariants variants;

        try
        {
            using var original = await s3.GetObjectAsync(options.BucketName, key);
            variants = await processor.ProcessAsync(original.ResponseStream);
        }
        catch (ImageFormatException ex)
        {
            logger.LogError(ex, "Photo {PhotoId} is not a supported image", parts.PhotoId);
            return;
        }

        var image = new ProcessedImage(
          S3Keys.Thumbnail(parts.PhotoId),
          S3Keys.Preview(parts.PhotoId),
          variants.Width,
          variants.Height
        );

        await UploadWebpAsync(image.ThumbnailKey, variants.Thumbnail);
        await UploadWebpAsync(image.PreviewKey, variants.Preview);

        if (await repo.MarkPhotoReadyAsync(parts.UserId, parts.AlbumId, parts.PhotoId, image))
            logger.LogInformation("Photo {PhotoId} processed ({Width}x{Height})", parts.PhotoId, variants.Width, variants.Height);
        else
            logger.LogWarning("Photo {PhotoId} was already processed or no longer exists", parts.PhotoId);

    }

    private Task UploadWebpAsync(string key, byte[] content) =>
          s3.PutObjectAsync(new PutObjectRequest
          {
              BucketName = options.BucketName,
              Key = key,
              InputStream = new MemoryStream(content),
              ContentType = "image/webp"
          });
}