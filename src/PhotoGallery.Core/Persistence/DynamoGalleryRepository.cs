using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Core.Persistence;

public sealed class DynamoGalleryRepository(IAmazonDynamoDB dynamoDB, GalleryOptions options) : IGalleryRepository
{
    private const int BatchWriteLimit = 25;
    private const string GsiAlbumShares = "GSI1";

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
                    ConditionCheck = new ConditionCheck
                    {
                        TableName = _table,
                        Key = Key(Keys.UserPk(photo.UserId), Keys.AlbumSk(photo.AlbumId)),
                        ConditionExpression = "attribute_exists(PK)"
                    }
                },
                // Tag pointers are written in the same transaction, so search never disagrees with the photo.
                .. photo.Tags.Select(tag => PutTagItem(photo, tag))
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

    public async Task<PhotoPage> ListPhotosPageAsync(
        string userId, string albumId, int limit, string? cursor, CancellationToken ct = default)
    {
        var pk = Keys.UserPk(userId);
        var prefix = Keys.PhotosInAlbumSkPrefix(albumId);

        Dictionary<string, AttributeValue>? startKey = null;
        if (cursor is not null)
        {
            if (!PageCursor.TryDecode(cursor, prefix, out var sortKey))
                throw new ArgumentException("Cursor does not belong to this album.", nameof(cursor));
            startKey = Key(pk, sortKey);
        }

        var response = await dynamoDB.QueryAsync(new QueryRequest
        {
            TableName = _table,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = pk.ToS(),
                [":prefix"] = prefix.ToS()
            },
            ExclusiveStartKey = startKey,
            Limit = limit,
            // Photo ids are UUIDv7, so descending sort key order is newest first.
            ScanIndexForward = false
        }, ct);

        var photos = (response.Items ?? []).Select(ItemMapper.ToPhoto).ToList();
        var nextCursor = response.LastEvaluatedKey is { Count: > 0 } last ? PageCursor.Encode(last["SK"].S) : null;

        return new PhotoPage(photos, nextCursor);
    }

    public async Task<bool> MarkPhotoReadyAsync(string userId, string albumId, string photoId, ProcessedImage image, CancellationToken ct = default)
    {
        try
        {
            await dynamoDB.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems =
                [
                    new TransactWriteItem
                    {
                        Update = new Update
                        {
                            TableName = _table,
                            Key = Key(Keys.UserPk(userId), Keys.PhotoSk(albumId, photoId)),
                            UpdateExpression =
                                "SET #status = :ready, ThumbnailKey = :thumb, PreviewKey = :preview, Width = :width, Height = :height REMOVE ExpiresAt",
                            ConditionExpression = "#status = :pending",
                            ExpressionAttributeNames = new() { ["#status"] = "Status" },
                            ExpressionAttributeValues = new()
                            {
                                [":ready"] = nameof(PhotoStatus.Ready).ToS(),
                                [":pending"] = nameof(PhotoStatus.Pending).ToS(),
                                [":thumb"] = image.ThumbnailKey.ToS(),
                                [":preview"] = image.PreviewKey.ToS(),
                                [":width"] = image.Width.ToN(),
                                [":height"] = image.Height.ToN()
                            }
                        }
                    },
                    new TransactWriteItem
                    {
                        Update = new Update
                        {
                            TableName = _table,
                            Key = Key(Keys.UserPk(userId), Keys.AlbumSk(albumId)),
                            UpdateExpression = "ADD PhotoCount :one SET CoverPhotoKey = if_not_exists(CoverPhotoKey, :thumb)",
                            ConditionExpression = "attribute_exists(PK)",
                            ExpressionAttributeValues = new()
                            {
                                [":one"] = 1.ToN(),
                                [":thumb"] = image.ThumbnailKey.ToS()
                            }
                        }
                    }
                ]
            }, ct);

            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }



    private async Task<Dictionary<string, AttributeValue>?> GetItemAsync(string pk, string sk, CancellationToken ct)
    {
        var response = await dynamoDB.GetItemAsync(new GetItemRequest
        {
            TableName = _table,
            Key = Key(pk, sk)
        }, ct);

        return response.Item is { Count: > 0 } item ? item : null;
    }

    private async Task<List<Dictionary<string, AttributeValue>>> QueryBySkPrefixAsync(
        string pk, string skPrefix, CancellationToken ct, string? indexName = null)
    {
        var results = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? startkey = null;

        do
        {
            var response = await dynamoDB.QueryAsync(new QueryRequest
            {
                TableName = _table,
                IndexName = indexName,
                KeyConditionExpression = indexName is null
                    ? "PK = :pk AND begins_with(SK, :prefix)"
                    : "GSI1PK = :pk AND begins_with(GSI1SK, :prefix)",
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

    public async Task<bool> MarkPhotoFailedAsync(string userId, string albumId, string photoId, CancellationToken ct = default)
    {
        try
        {
            await dynamoDB.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _table,
                Key = Key(Keys.UserPk(userId), Keys.PhotoSk(albumId, photoId)),
                UpdateExpression = "SET #status = :failed REMOVE ExpiresAt",
                ConditionExpression = "#status = :pending",
                ExpressionAttributeNames = new() { ["#status"] = "Status" },
                ExpressionAttributeValues = new()
                {
                    [":failed"] = nameof(PhotoStatus.Failed).ToS(),
                    [":pending"] = nameof(PhotoStatus.Pending).ToS()
                }
            }, ct);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<bool> DeletePhotoAsync(Photo photo, CancellationToken ct = default)
    {
        List<TransactWriteItem> items =
        [
            new TransactWriteItem
        {
            Delete = new Delete
            {
                TableName = _table,
                Key = Key(Keys.UserPk(photo.UserId), Keys.PhotoSk(photo.AlbumId, photo.PhotoId)),
                ConditionExpression = "#status = :status",
                ExpressionAttributeNames = new() {["#status"] = "Status"},
                ExpressionAttributeValues = new() {[":status"] = photo.Status.ToString().ToS()}
            }
        }
        ];

        if (photo.Status == PhotoStatus.Ready)
        {
            items.Add(new TransactWriteItem
            {
                Update = new Update
                {
                    TableName = _table,
                    Key = Key(Keys.UserPk(photo.UserId), Keys.AlbumSk(photo.AlbumId)),
                    UpdateExpression = "ADD PhotoCount :minusOne",
                    ConditionExpression = "attribute_exists(PK)",
                    ExpressionAttributeValues = new() { [":minusOne"] = (-1).ToN() }
                }
            });
        }

        items.AddRange(photo.Tags.Select(tag => DeleteTagItem(photo, tag)));

        try
        {
            await dynamoDB.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = items,

            }, ct);
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }

    public Task SetAlbumCoverAsync(string userId, string albumId, string? coverPhotoKey, CancellationToken ct = default)
    => dynamoDB.UpdateItemAsync(new UpdateItemRequest
    {
        TableName = _table,
        Key = Key(Keys.UserPk(userId), Keys.AlbumSk(albumId)),
        UpdateExpression = coverPhotoKey is null ? "REMOVE CoverPhotoKey" : "SET CoverPhotoKey = :cover",
        ConditionExpression = "attribute_exists(PK)",
        ExpressionAttributeValues = coverPhotoKey is null ? null : new() { [":cover"] = coverPhotoKey.ToS() }
    }, ct);

    public Task CreateShareAsync(Share share, CancellationToken ct = default) =>
          dynamoDB.TransactWriteItemsAsync(new TransactWriteItemsRequest
          {
              TransactItems =
              [
                  new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _table,
                        Item = ItemMapper.ToItem(share),
                        ConditionExpression = "attribute_not_exists(PK)"
                    }
                },
                new TransactWriteItem
                {
                    ConditionCheck = new ConditionCheck
                    {
                        TableName = _table,
                        Key = Key(Keys.UserPk(share.OwnerUserId), Keys.AlbumSk(share.AlbumId)),
                        ConditionExpression = "attribute_exists(PK)"
                    }
                }
              ]
          }, ct);

    public async Task<Share?> GetShareAsync(string code, CancellationToken ct = default)
    {
        var item = await GetItemAsync(Keys.SharePk(code), Keys.ShareSk, ct);
        return item is null ? null : ItemMapper.ToShare(item);
    }

    public async Task<IReadOnlyList<Share>> ListSharesAsync(string userId, string albumId, CancellationToken ct = default)
    {
        var items = await QueryBySkPrefixAsync(Keys.AlbumGsiPk(albumId), Keys.ShareGsiSkPrefix, ct, GsiAlbumShares);

        // Album ids are unguessable, but the owner check keeps the guarantee explicit.
        return items.Select(ItemMapper.ToShare).Where(share => share.OwnerUserId == userId).ToList();
    }

    public async Task<bool> DeleteShareAsync(string userId, string code, CancellationToken ct = default)
    {
        try
        {
            await dynamoDB.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = _table,
                Key = Key(Keys.SharePk(code), Keys.ShareSk),
                ConditionExpression = "OwnerUserId = :owner",
                ExpressionAttributeValues = new() { [":owner"] = userId.ToS() }
            }, ct);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<bool> RenameAlbumAsync(string userId, string albumId, string name, CancellationToken ct = default)
    {
        try
        {
            await dynamoDB.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _table,
                Key = Key(Keys.UserPk(userId), Keys.AlbumSk(albumId)),
                UpdateExpression = "SET #name = :name",
                ConditionExpression = "attribute_exists(PK)",
                ExpressionAttributeNames = new() { ["#name"] = "Name" },
                ExpressionAttributeValues = new() { [":name"] = name.ToS() }
            }, ct);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<Photo>?> DeleteAlbumAsync(string userId, string albumId, CancellationToken ct = default)
    {
        if (await GetAlbumAsync(userId, albumId, ct) is null)
            return null;

        var photos = await ListPhotosAsync(userId, albumId, ct);
        var shares = await ListSharesAsync(userId, albumId, ct);

        // Share links go with the album, otherwise they would linger as dead records until TTL.
        var keys = photos
            .Select(photo => Key(Keys.UserPk(userId), Keys.PhotoSk(albumId, photo.PhotoId)))
            .Concat(photos.SelectMany(photo => photo.Tags.Select(tag => Key(Keys.UserPk(userId), Keys.TagSk(tag, photo.PhotoId)))))
            .Concat(shares.Select(share => Key(Keys.SharePk(share.Code), Keys.ShareSk)))
            .Append(Key(Keys.UserPk(userId), Keys.AlbumSk(albumId)));

        foreach (var chunk in keys.Chunk(BatchWriteLimit))
            await BatchDeleteAsync(chunk, ct);

        return photos;
    }

    public async Task<bool> UpdatePhotoDetailsAsync(
        string userId, string albumId, string photoId, string? caption, IReadOnlyList<string> tags, CancellationToken ct = default)
    {
        if (await GetPhotoAsync(userId, albumId, photoId, ct) is not { } photo)
            return false;

        var values = new Dictionary<string, AttributeValue>
        {
            [":tags"] = new() { L = tags.Select(tag => tag.ToS()).ToList() }
        };
        if (caption is not null)
            values[":caption"] = caption.ToS();

        // Tag pointers follow the photo: drop pointers for removed tags, add pointers for new ones.
        var oldKeys = photo.Tags.ToDictionary(Keys.TagKey);
        var newKeys = tags.ToDictionary(Keys.TagKey);

        List<TransactWriteItem> items =
        [
            new TransactWriteItem
            {
                Update = new Update
                {
                    TableName = _table,
                    Key = Key(Keys.UserPk(userId), Keys.PhotoSk(albumId, photoId)),
                    UpdateExpression = caption is null
                        ? "SET Tags = :tags REMOVE Caption"
                        : "SET Tags = :tags, Caption = :caption",
                    ConditionExpression = "attribute_exists(PK)",
                    ExpressionAttributeValues = values
                }
            },
            .. oldKeys.Where(old => !newKeys.ContainsKey(old.Key)).Select(old => DeleteTagItem(photo, old.Value)),
            // Put also refreshes the display text when only the casing of a tag changed.
            .. newKeys.Values.Select(tag => PutTagItem(photo, tag))
        ];

        try
        {
            await dynamoDB.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = items }, ct);
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }

    public async Task<PhotoPage> SearchByTagAsync(
        string userId, string tag, int limit, string? cursor, CancellationToken ct = default)
    {
        var pk = Keys.UserPk(userId);
        var prefix = Keys.PhotosWithTagSkPrefix(tag);

        Dictionary<string, AttributeValue>? startKey = null;
        if (cursor is not null)
        {
            if (!PageCursor.TryDecode(cursor, prefix, out var sortKey))
                throw new ArgumentException("Cursor does not belong to this tag.", nameof(cursor));
            startKey = Key(pk, sortKey);
        }

        var response = await dynamoDB.QueryAsync(new QueryRequest
        {
            TableName = _table,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :prefix)",
            ExpressionAttributeValues = new() { [":pk"] = pk.ToS(), [":prefix"] = prefix.ToS() },
            ExclusiveStartKey = startKey,
            Limit = limit,
            ScanIndexForward = false // UUIDv7 photo ids: newest first
        }, ct);

        var pointers = (response.Items ?? []).Select(ItemMapper.ToTagPointer).ToList();
        var photos = await BatchGetPhotosAsync(userId, pointers.Select(p => (p.AlbumId, p.PhotoId)).ToList(), ct);
        var nextCursor = response.LastEvaluatedKey is { Count: > 0 } last ? PageCursor.Encode(last["SK"].S) : null;

        return new PhotoPage(photos, nextCursor);
    }

    public async Task<IReadOnlyList<TagCount>> ListTagsAsync(string userId, CancellationToken ct = default)
    {
        var items = await QueryBySkPrefixAsync(Keys.UserPk(userId), Keys.TagSkPrefix, ct);

        return items
            .Select(ItemMapper.ToTagPointer)
            .GroupBy(pointer => Keys.TagKey(pointer.Tag))
            .Select(group => new TagCount(group.First().Tag, group.Count()))
            .OrderByDescending(tag => tag.Count)
            .ThenBy(tag => tag.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private TransactWriteItem PutTagItem(Photo photo, string tag) =>
        new() { Put = new Put { TableName = _table, Item = ItemMapper.ToTagItem(photo, tag) } };

    private TransactWriteItem DeleteTagItem(Photo photo, string tag) =>
        new() { Delete = new Delete { TableName = _table, Key = Key(Keys.UserPk(photo.UserId), Keys.TagSk(tag, photo.PhotoId)) } };

    /// <summary>Loads photos by key, preserving the requested order. Missing photos are skipped.</summary>
    private async Task<IReadOnlyList<Photo>> BatchGetPhotosAsync(
        string userId, IReadOnlyList<(string AlbumId, string PhotoId)> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return [];

        var found = new Dictionary<string, Photo>();
        var pending = new KeysAndAttributes { Keys = ids.Select(id => Key(Keys.UserPk(userId), Keys.PhotoSk(id.AlbumId, id.PhotoId))).ToList() };

        for (var attempt = 0; pending.Keys is { Count: > 0 }; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), ct);

            var response = await dynamoDB.BatchGetItemAsync(
                new BatchGetItemRequest { RequestItems = new() { [_table] = pending } }, ct);

            foreach (var item in response.Responses?.GetValueOrDefault(_table) ?? [])
            {
                var photo = ItemMapper.ToPhoto(item);
                found[photo.PhotoId] = photo;
            }

            pending = response.UnprocessedKeys?.GetValueOrDefault(_table) ?? new KeysAndAttributes();
        }

        return ids.Where(id => found.ContainsKey(id.PhotoId)).Select(id => found[id.PhotoId]).ToList();
    }

    private async Task BatchDeleteAsync(IEnumerable<Dictionary<string, AttributeValue>> keys, CancellationToken ct)
    {
        var requests = keys.Select(key => new WriteRequest { DeleteRequest = new DeleteRequest { Key = key } }).ToList();
        var pending = new Dictionary<string, List<WriteRequest>> { [_table] = requests };

        for (var attempt = 0; pending.Count > 0; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), ct);

            var response = await dynamoDB.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = pending }, ct);
            pending = response.UnprocessedItems is { Count: > 0 } unprocessed ? unprocessed : [];
        }
    }
}