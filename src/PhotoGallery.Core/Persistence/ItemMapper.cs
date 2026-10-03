using Amazon.DynamoDBv2.Model;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Core.Persistence;

public static class ItemMapper
{
    public const string AlbumType = "Album";
    public const string PhotoType = "Photo";
    public const string ShareType = "Share";
    public const string PhotoTagType = "PhotoTag";
    public static readonly TimeSpan PendingTtl = TimeSpan.FromHours(24);
    public static Dictionary<string, AttributeValue> ToItem(Album album)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = Keys.UserPk(album.UserId).ToS(),
            ["SK"] = Keys.AlbumSk(album.AlbumId).ToS(),
            ["EntityType"] = AlbumType.ToS(),
            ["UserId"] = album.UserId.ToS(),
            ["AlbumId"] = album.AlbumId.ToS(),
            ["Name"] = album.Name.ToS(),
            ["PhotoCount"] = album.PhotoCount.ToN(),
            ["CreatedAt"] = album.CreatedAt.ToS()
        };

        if (album.CoverPhotoKey is not null)
            item["CoverPhotoKey"] = album.CoverPhotoKey.ToS();

        return item;
    }

    public static Album ToAlbum(Dictionary<string, AttributeValue> item) => new()
    {
        UserId = item.GetString("UserId"),
        AlbumId = item.GetString("AlbumId"),
        Name = item.GetString("Name"),
        CoverPhotoKey = item.GetStringOrNull("CoverPhotoKey"),
        PhotoCount = item.GetInt("PhotoCount"),
        CreatedAt = item.GetDate("CreatedAt")
    };

    public static Dictionary<string, AttributeValue> ToItem(Photo photo)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = Keys.UserPk(photo.UserId).ToS(),
            ["SK"] = Keys.PhotoSk(photo.AlbumId, photo.PhotoId).ToS(),
            ["EntityType"] = PhotoType.ToS(),
            ["UserId"] = photo.UserId.ToS(),
            ["AlbumId"] = photo.AlbumId.ToS(),
            ["PhotoId"] = photo.PhotoId.ToS(),
            ["OriginalKey"] = photo.OriginalKey.ToS(),
            ["Tags"] = new AttributeValue { L = photo.Tags.Select(t => t.ToS()).ToList() },
            ["Status"] = photo.Status.ToString().ToS(),
            ["CreatedAt"] = photo.CreatedAt.ToS()
        };

        if (photo.ThumbnailKey is not null) item["ThumbnailKey"] = photo.ThumbnailKey.ToS();
        if (photo.PreviewKey is not null) item["PreviewKey"] = photo.PreviewKey.ToS();
        if (photo.Caption is not null) item["Caption"] = photo.Caption.ToS();
        if (photo.Width is { } width) item["Width"] = width.ToN();
        if (photo.Height is { } height) item["Height"] = height.ToN();

        if (photo.Status == PhotoStatus.Pending)
            item["ExpiresAt"] = photo.CreatedAt.Add(PendingTtl).ToUnixTimeSeconds().ToN();

        return item;
    }

    public static Photo ToPhoto(Dictionary<string, AttributeValue> item) => new()
    {
        UserId = item.GetString("UserId"),
        AlbumId = item.GetString("AlbumId"),
        PhotoId = item.GetString("PhotoId"),
        OriginalKey = item.GetString("OriginalKey"),
        ThumbnailKey = item.GetStringOrNull("ThumbnailKey"),
        PreviewKey = item.GetStringOrNull("PreviewKey"),
        Caption = item.GetStringOrNull("Caption"),
        Tags = item.GetStringList("Tags"),
        Status = Enum.Parse<PhotoStatus>(item.GetString("Status")),
        Width = item.GetIntOrNull("Width"),
        Height = item.GetIntOrNull("Height"),
        CreatedAt = item.GetDate("CreatedAt")
    };

    public static Dictionary<string, AttributeValue> ToItem(Share share)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = Keys.SharePk(share.Code).ToS(),
            ["SK"] = Keys.ShareSk.ToS(),
            // GSI1: all share links of an album (list, revoke, delete with the album).
            ["GSI1PK"] = Keys.AlbumGsiPk(share.AlbumId).ToS(),
            ["GSI1SK"] = Keys.ShareGsiSk(share.Code).ToS(),
            ["EntityType"] = ShareType.ToS(),
            ["Code"] = share.Code.ToS(),
            ["OwnerUserId"] = share.OwnerUserId.ToS(),
            ["AlbumId"] = share.AlbumId.ToS(),
            ["CreatedAt"] = share.CreatedAt.ToS()
        };

        if (share.ExpiresAt is { } expiresAt)
            item["ExpiresAt"] = expiresAt.ToUnixTimeSeconds().ToN();

        return item;
    }

    public static Share ToShare(Dictionary<string, AttributeValue> item) => new()
    {
        Code = item.GetString("Code"),
        OwnerUserId = item.GetString("OwnerUserId"),
        AlbumId = item.GetString("AlbumId"),
        CreatedAt = item.GetDate("CreatedAt"),
        ExpiresAt = item.GetLongOrNull("ExpiresAt") is { } seconds ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null
    };

    /// <summary>Pointer item that makes "photos with tag X" a single Query instead of a scan.</summary>
    public static Dictionary<string, AttributeValue> ToTagItem(Photo photo, string tag) => new()
    {
        ["PK"] = Keys.UserPk(photo.UserId).ToS(),
        ["SK"] = Keys.TagSk(tag, photo.PhotoId).ToS(),
        ["EntityType"] = PhotoTagType.ToS(),
        ["Tag"] = tag.ToS(),
        ["AlbumId"] = photo.AlbumId.ToS(),
        ["PhotoId"] = photo.PhotoId.ToS()
    };

    public static (string Tag, string AlbumId, string PhotoId) ToTagPointer(Dictionary<string, AttributeValue> item) =>
        (item.GetString("Tag"), item.GetString("AlbumId"), item.GetString("PhotoId"));
}
