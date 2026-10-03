using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.TestUtilities;
using PhotoGallery.Core;
using PhotoGallery.Processing;
using PhotoGallery.Tests.Photos;

namespace PhotoGallery.Tests.Processing;

public class ExpiredUploadCleanupFunctionTests
{
    private readonly FakePhotoStorage _storage = new();

    [Fact]
    public async Task TtlExpiryOfPendingPhoto_DeletesOriginalAndVariants()
    {
        await Handle(Record(Ttl, Photo("Pending", "p1")));

        Assert.Equivalent(
            new[] { "originals/u1/a1/p1/x.jpg", S3Keys.Thumbnail("p1"), S3Keys.Preview("p1") },
            _storage.DeletedKeys,
            strict: true);
    }

    [Fact]
    public async Task UserDelete_IsIgnored()
    {
        await Handle(Record(new DynamoDBEvent.Identity(), Photo("Pending", "p1")));

        Assert.Empty(_storage.DeletedKeys);
    }

    [Theory]
    [InlineData("Ready")]
    [InlineData("Failed")]
    public async Task TtlExpiryOfNonPendingPhoto_IsIgnored(string status)
    {
        await Handle(Record(Ttl, Photo(status, "p1")));

        Assert.Empty(_storage.DeletedKeys);
    }

    [Fact]
    public async Task TtlExpiryOfShare_IsIgnored()
    {
        await Handle(Record(Ttl, new() { ["EntityType"] = S("Share"), ["Code"] = S("abc") }));

        Assert.Empty(_storage.DeletedKeys);
    }

    private static readonly DynamoDBEvent.Identity Ttl = new() { Type = "Service", PrincipalId = "dynamodb.amazonaws.com" };

    private Task Handle(DynamoDBEvent.DynamodbStreamRecord record) =>
        new ExpiredUploadCleanupFunction(_storage).HandleAsync(new DynamoDBEvent { Records = [record] }, new TestLambdaContext());

    private static DynamoDBEvent.DynamodbStreamRecord Record(
        DynamoDBEvent.Identity identity, Dictionary<string, DynamoDBEvent.AttributeValue> oldImage) => new()
        {
            EventName = "REMOVE",
            UserIdentity = identity,
            Dynamodb = new DynamoDBEvent.StreamRecord { OldImage = oldImage }
        };

    private static Dictionary<string, DynamoDBEvent.AttributeValue> Photo(string status, string photoId) => new()
    {
        ["EntityType"] = S("Photo"),
        ["Status"] = S(status),
        ["PhotoId"] = S(photoId),
        ["OriginalKey"] = S($"originals/u1/a1/{photoId}/x.jpg")
    };

    private static DynamoDBEvent.AttributeValue S(string value) => new() { S = value };
}
