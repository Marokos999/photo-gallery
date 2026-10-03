using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.S3.Model;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;
using SixLabors.ImageSharp;
using Metrics = AWS.Lambda.Powertools.Metrics.Metrics;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<PhotoGallery.Processing.ProcessingJsonContext>))]

namespace PhotoGallery.Processing;

public sealed class ProcessingFunction
{

    private readonly IGalleryRepository repo;
    private readonly IAmazonS3 s3;
    private readonly ImageProcessor processor;
    private readonly GalleryOptions options;

    static ProcessingFunction()
    {
        if (Observability.IsRunningInLambda)
            Tracing.RegisterForAllServices();
    }

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

    [Logging(ClearState = true)]
    [Metrics(Namespace = Observability.MetricsNamespace, CaptureColdStart = true)]
    [Tracing]
    public async Task HandleAsync(S3Event s3Event, ILambdaContext context)
    {
        foreach (var record in s3Event.Records ?? [])
        {
            await ProcessObjectAsync(record.S3.Object);
        }

    }

    private async Task ProcessObjectAsync(S3Event.S3ObjectEntity s3Object)
    {
        var key = s3Object.KeyDecoded;

        if (!S3Keys.TryParseOriginal(key, out var parts))
        {
            Logger.LogWarning("Ignoring object with unexpected key {Key}", key);
            return;
        }

        Logger.AppendKey("photoId", parts.PhotoId);

        var existing = await repo.GetPhotoAsync(parts.UserId, parts.AlbumId, parts.PhotoId);
        if (existing is not { Status: PhotoStatus.Pending })
        {
            Logger.LogInformation("Photo {PhotoId} is not pending, skipping", parts.PhotoId);
            return;
        }

        if (s3Object.Size > UploadLimits.MaxFileBytes)
        {
            Logger.LogWarning("Photo {PhotoId} is too large ({Size} bytes)", parts.PhotoId, s3Object.Size);
            await MarkFailedAsync(parts);
            return;
        }

        ProcessedVariants variants;

        try
        {
            using var original = await s3.GetObjectAsync(options.BucketName, key);
            variants = await processor.ProcessAsync(original.ResponseStream);
        }
        catch (Exception ex) when (ex is ImageFormatException or ImageTooLargeException)
        {
            // Not retryable: the same bytes will fail again. Record it so the UI stops waiting.
            Logger.LogError(ex, "Photo {PhotoId} was rejected: {Reason}", parts.PhotoId, ex.Message);
            await MarkFailedAsync(parts);
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
        {
            Logger.LogInformation("Photo {PhotoId} processed ({Width}x{Height})", parts.PhotoId, variants.Width, variants.Height);
            Metrics.AddMetric("PhotosProcessed", 1, MetricUnit.Count);
            Metrics.AddMetric("OriginalBytes", s3Object.Size, MetricUnit.Bytes);
        }
        else
            Logger.LogWarning("Photo {PhotoId} was already processed or no longer exists", parts.PhotoId);

    }

    private async Task MarkFailedAsync(OriginalKeyParts parts)
    {
        await repo.MarkPhotoFailedAsync(parts.UserId, parts.AlbumId, parts.PhotoId);
        Metrics.AddMetric("PhotosFailed", 1, MetricUnit.Count);
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
