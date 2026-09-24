using PhotoGallery.Core;

namespace PhotoGallery.Tests;

public class KeysTests
{
    [Fact]
    public void UserPk_HasUserPrefix() =>
        Assert.Equal("USER#u1", Keys.UserPk("u1"));

    [Fact]
    public void PhotoSk_ContainsAlbumAndPhoto() =>
        Assert.Equal("PHOTO#a1#p1", Keys.PhotoSk("a1", "p1"));

    [Fact]
    public void PhotoSk_StartsWithAlbumPrefix() =>
        Assert.StartsWith(Keys.PhotosInAlbumSkPrefix("a1"), Keys.PhotoSk("a1", "p1"));

    [Fact]
    public void NewId_IsSortableByCreationTime()
    {
        var first = Keys.NewId();
        var second = Keys.NewId();

        Assert.True(string.CompareOrdinal(first, second) < 0);
    }
}