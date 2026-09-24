using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Core.Persistence;

public sealed class DynamoGalleryRepository(IAmazonDynamoDB dynamoDB, GalleryOptions options) : IGalleryRepository
{
    private readonly string _table = options.TableName;
    public Task CreateAlbumAsync(Album album, CancellationToken ct = default) =>
    dynamoDB.PutItemAsync(new PutItemRequest
    {
        TableName = _table,
        Item = ItemMapper.ToItem(album),
        ConditionExpression = "attribute_not_exists(PK)"
    }, ct);

    public Task CreatePhotoAsync(Photo photo, CancellationToken ct = default) =>
        dynamoDB.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems =
            [
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _table,
                        Item = ItemMapper.ToItem(photo),
                        ConditionExpression = "attribute_not_exists(PK)"
                    }
                },
                new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = _table,
                        Key = Key(Keys.UserPk(photo.UserId), Keys.AlbumSk(photo.AlbumId)),
                        UpdateExpression = "ADD PhotoCount :one",
                        ConditionExpression = "attribute_exists(PK)",
                        ExpressionAttributeValues = new() { [":one"] = 1.ToN() }
                    }
                }
            ]
        }, ct);

    public async Task<Album?> GetAlbumAsync(string userId, string albumId, CancellationToken ct = default)
    {
        var item = await GetItemAsync(Keys.UserPk(userId), Keys.AlbumSk(albumId), ct);
        return item is null ? null : ItemMapper.ToAlbum(item);
    }

    public async Task<Photo?> GetPhotoAsync(string userId, string albumId, string photoId, CancellationToken ct = default)
    {
        var item = await GetItemAsync(Keys.UserPk(userId), Keys.PhotoSk(albumId, photoId), ct);
        return item is null ? null : ItemMapper.ToPhoto(item);
    }

    public async Task<IReadOnlyList<Album>> ListAlbumsAsync(string userId, CancellationToken ct = default)
    {
        var items = await QueryBySkPrefixAsync(Keys.UserPk(userId), Keys.AlbumSkPrefix, ct);
        return items.Select(ItemMapper.ToAlbum).ToList();
    }

    public async Task<IReadOnlyList<Photo>> ListPhotosAsync(string userId, string albumId, CancellationToken ct = default)
    {
        var items = await QueryBySkPrefixAsync(Keys.UserPk(userId), Keys.PhotosInAlbumSkPrefix(albumId), ct);
        return items.Select(ItemMapper.ToPhoto).ToList();
    }

    public Task MarkPhotoReadyAsync(string userId, string albumId, string photoId, ProcessedImage image, CancellationToken ct = default)
    => dynamoDB.UpdateItemAsync(new UpdateItemRequest
    {
        TableName = _table,
        Key = Key(Keys.UserPk(userId), Keys.PhotoSk(albumId, photoId)),
        UpdateExpression = "SET #status = :ready, ThumbnailKey = :thumb, PreviewKey = :preview, Width = :width, Height = :height",
        ConditionExpression = "attribute_exists(PK)",
        ExpressionAttributeNames = new() { ["#status"] = "Status" },
        ExpressionAttributeValues = new()
        {
            [":ready"] = nameof(PhotoStatus.Ready).ToS(),
            [":thumb"] = image.ThumbnailKey.ToS(),
            [":preview"] = image.PreviewKey.ToS(),
            [":width"] = image.Width.ToN(),
            [":height"] = image.Height.ToN()
        }
    }, ct);



    private async Task<Dictionary<string, AttributeValue>?> GetItemAsync(string pk, string sk, CancellationToken ct)
    {
        var response = await dynamoDB.GetItemAsync(new GetItemRequest
        {
            TableName = _table,
            Key = Key(pk, sk)
        }, ct);

        return response.Item is { Count: > 0 } item ? item : null;
    }

    private async Task<List<Dictionary<string, AttributeValue>>> QueryBySkPrefixAsync(string pk, string skPrefix, CancellationToken ct)
    {
        var results = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? startkey = null;

        do
        {
            var response = await dynamoDB.QueryAsync(new QueryRequest
            {
                TableName = _table,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
                ExpressionAttributeValues = new()
                {
                    [":pk"] = pk.ToS(),
                    [":prefix"] = skPrefix.ToS()
                },
                ExclusiveStartKey = startkey
            }, ct);

            if (response.Items is { } items) results.AddRange(items);

            startkey = response.LastEvaluatedKey is { Count: > 0 } lastKey ? lastKey : null;
        } while (startkey is not null);

        return results;
    }

    private static Dictionary<string, AttributeValue> Key(string pk, string sk) => new()
    {
        ["PK"] = pk.ToS(),
        ["SK"] = sk.ToS()
    };
}