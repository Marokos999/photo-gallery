using PhotoGallery.Core.Models;
using PhotoGallery.Core.Persistence;

namespace PhotoGallery.Tests;

public class ItemMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Album_RoundTrips()
    {
        var album = new Album { UserId = "u1", AlbumId = "a1", Name = "Summer", PhotoCount = 3, CreatedAt = Now };

        var item = ItemMapper.ToItem(album);

        Assert.Equal("USER#u1", item["PK"].S);
        Assert.Equal("ALBUM#a1", item["SK"].S);
        Assert.False(item.ContainsKey("CoverPhotoKey"));
        Assert.Equivalent(album, ItemMapper.ToAlbum(item));
    }

    [Fact]
    public void Photo_RoundTrips()
    {
        var photo = new Photo
        {
            UserId = "u1",
            AlbumId = "a1",
            PhotoId = "p1",
            OriginalKey = "originals/u1/a1/p1/beach.jpg",
            ThumbnailKey = "thumbs/p1_400.webp",
            Caption = "Beach",
            Tags = ["sea", "sun"],
            Status = PhotoStatus.Ready,
            Width = 4000,
            Height = 3000,
            CreatedAt = Now
        };

        Assert.Equivalent(photo, ItemMapper.ToPhoto(ItemMapper.ToItem(photo)));
    }

    [Fact]
    public void Photo_WithoutTags_RoundTripsToEmptyList()
    {
        var photo = new Photo { UserId = "u1", AlbumId = "a1", PhotoId = "p1", OriginalKey = "k", CreatedAt = Now };

        var mapped = ItemMapper.ToPhoto(ItemMapper.ToItem(photo));

        Assert.Empty(mapped.Tags);
        Assert.Null(mapped.Width);
    }

    [Fact]
    public void Photo_HasAlbumGsiKeys()
    {
        var photo = new Photo { UserId = "u1", AlbumId = "a1", PhotoId = "p1", OriginalKey = "k", CreatedAt = Now };

        var item = ItemMapper.ToItem(photo);

        Assert.Equal("PHOTO#a1#p1", item["SK"].S);
        Assert.Equal("ALBUM#a1", item["GSI1PK"].S);
        Assert.Equal("PHOTO#p1", item["GSI1SK"].S);
    }
    [Fact]
    public void PendingPhoto_ExpiresAfter24Hours()
    {
        var photo = new Photo { UserId = "u1", AlbumId = "a1", PhotoId = "p1", OriginalKey = "k", CreatedAt = Now };

        var item = ItemMapper.ToItem(photo);

        Assert.Equal(Now.AddHours(24).ToUnixTimeSeconds().ToString(), item["ExpiresAt"].N);
    }

    [Fact]
    public void ReadyPhoto_HasNoExpiry()
    {
        var photo = new Photo
        {
            UserId = "u1",
            AlbumId = "a1",
            PhotoId = "p1",
            OriginalKey = "k",
            Status = PhotoStatus.Ready,
            CreatedAt = Now
        };

        Assert.False(ItemMapper.ToItem(photo).ContainsKey("ExpiresAt"));
    }

    [Fact]
    public void Share_WithExpiry_RoundTrips()
    {
        var share = new Share
        {
            Code = "abc",
            OwnerUserId = "u1",
            AlbumId = "a1",
            CreatedAt = Now,
            ExpiresAt = Now.AddDays(7)
        };

        var item = ItemMapper.ToItem(share);

        Assert.Equal("SHARE#abc", item["PK"].S);
        Assert.Equivalent(share, ItemMapper.ToShare(item));
    }

    [Fact]
    public void Share_WithoutExpiry_NeverExpires()
    {
        var share = new Share { Code = "abc", OwnerUserId = "u1", AlbumId = "a1", CreatedAt = Now };

        var mapped = ItemMapper.ToShare(ItemMapper.ToItem(share));

        Assert.Null(mapped.ExpiresAt);
        Assert.False(mapped.IsExpired(Now.AddYears(10)));
    }
}