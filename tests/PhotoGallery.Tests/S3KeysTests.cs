using PhotoGallery.Core;

namespace PhotoGallery.Tests;

public class S3KeysTests
{
    [Fact]
    public void Original_RoundTripsThroughParse()
    {
        var key = S3Keys.Original("u1", "a1", "p1", "beach.jpg");

        Assert.True(S3Keys.TryParseOriginal(key, out var parts));
        Assert.Equal(new OriginalKeyParts("u1", "a1", "p1", "beach.jpg"), parts);
    }

    [Theory]
    [InlineData("thumbs/p1_400.webp")]
    [InlineData("originals/u1/a1/beach.jpg")]
    [InlineData("originals/u1/a1/p1/extra/beach.jpg")]
    [InlineData("originals/u1//p1/beach.jpg")]
    public void TryParseOriginal_RejectsInvalidKeys(string key) =>
        Assert.False(S3Keys.TryParseOriginal(key, out _));

    [Fact]
    public void Thumbnail_And_Preview_AreWebp()
    {
        Assert.Equal("thumbs/p1_400.webp", S3Keys.Thumbnail("p1"));
        Assert.Equal("thumbs/p1_1200.webp", S3Keys.Preview("p1"));
    }
}