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
        var now = DateTimeOffset.UtcNow;
        var first = Keys.NewId(now);
        var second = Keys.NewId(now.AddMilliseconds(1));

        Assert.True(string.CompareOrdinal(first, second) < 0);
    }

    [Fact]
    public void NewShareCode_IsUrlSafeAndUnique()
    {
        var code = Keys.NewShareCode();

        Assert.Equal(22, code.Length);
        Assert.DoesNotContain(code, c => c is '+' or '/' or '=');
        Assert.NotEqual(code, Keys.NewShareCode());
    }
}