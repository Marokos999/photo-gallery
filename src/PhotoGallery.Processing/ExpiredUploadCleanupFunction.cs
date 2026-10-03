using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using PhotoGallery.Core;
using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;

namespace PhotoGallery.Processing;

/// <summary>
/// DynamoDB Streams consumer. A photo record is written as Pending before the browser uploads and expires
/// through TTL after 24 h if processing never marked it Ready. When TTL removes such a record, this function
/// deletes whatever reached S3 for it (an uploaded original, or variants from a processing run that failed midway),
/// so no orphaned objects are left behind. Deletions made by users are ignored: the API cleans those up itself.
/// </summary>
public sealed class ExpiredUploadCleanupFunction
{
    private const string DynamoDbServicePrincipal = "dynamodb.amazonaws.com";

    private readonly IPhotoStorage _storage;

    public ExpiredUploadCleanupFunction() : this(GalleryOptions.FromEnvironment())
    {
    }

    private ExpiredUploadCleanupFunction(GalleryOptions options)
        : this(new S3PhotoStorage(AwsClientFactory.CreateS3(options), options))
    {
    }

    public ExpiredUploadCleanupFunction(IPhotoStorage storage) => _storage = storage;

    public async Task HandleAsync(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        var objectKeys = new List<string>();

        foreach (var record in dynamoEvent.Records ?? [])
        {
            if (!IsTtlExpiry(record) || record.Dynamodb?.OldImage is not { } image)
                continue;

            if (Read(image, "EntityType") != ItemMapper.PhotoType
                || Read(image, "Status") != nameof(PhotoStatus.Pending)
                || Read(image, "PhotoId") is not { } photoId
                || Read(image, "OriginalKey") is not { } originalKey)
                continue;

            objectKeys.AddRange([originalKey, S3Keys.Thumbnail(photoId), S3Keys.Preview(photoId)]);
            context.Logger.LogInformation("Pending upload {PhotoId} expired; removing its S3 objects", photoId);
        }

        if (objectKeys.Count > 0)
            await _storage.DeleteAsync(objectKeys);
    }

    /// <summary>TTL deletions are performed by the DynamoDB service itself; user deletes carry no such identity.</summary>
    public static bool IsTtlExpiry(DynamoDBEvent.DynamodbStreamRecord record) =>
        record.EventName == "REMOVE"
        && record.UserIdentity is { Type: "Service", PrincipalId: DynamoDbServicePrincipal };

    private static string? Read(Dictionary<string, DynamoDBEvent.AttributeValue> image, string name) =>
        image.TryGetValue(name, out var value) ? value.S : null;
}
