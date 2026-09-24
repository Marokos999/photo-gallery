using Amazon.DynamoDBv2.Model;
using PhotoGallery.Core.Models;

namespace PhotoGallery.Core.Persistence;

public static class ItemMapper
{
    public const string AlbumType = "Album";
    public const string PhotoType = "Photo";
    public const string AlbumGsiSk = "ALBUM";
    public static readonly TimeSpan PendingTtl = TimeSpan.FromHours(24);
    public static Dictionary<string, AttributeValue> ToItem(Album album)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = Keys.UserPk(album.UserId).ToS(),
            ["SK"] = Keys.AlbumSk(album.AlbumId).ToS(),
            ["GSI1PK"] = Keys.AlbumGsiPk(album.AlbumId).ToS(),
            ["GSI1SK"] = AlbumGsiSk.ToS(),
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
            ["GSI1PK"] = Keys.AlbumGsiPk(photo.AlbumId).ToS(),
            ["GSI1SK"] = Keys.PhotoGsiSk(photo.PhotoId).ToS(),
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
}