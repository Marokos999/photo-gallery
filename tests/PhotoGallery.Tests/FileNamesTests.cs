using PhotoGallery.Core.Storage;

namespace PhotoGallery.Tests;

public class FileNamesTests
{
    [Theory]
    [InlineData("beach.jpg", "beach.jpg")]
    [InlineData("My Photo (1).JPG", "My-Photo-1-.JPG")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"..\..\windows\evil.png", "evil.png")]
    [InlineData("???", "photo")]
    public void Sanitize_ProducesSafeFileName(string input, string expected) =>
        Assert.Equal(expected, FileNames.Sanitize(input));

    [Fact]
    public void Sanitize_TruncatesLongNames_KeepingExtension()
    {
        var result = FileNames.Sanitize(new string('a', 200) + ".jpg");

        Assert.Equal(100, result.Length);
        Assert.EndsWith(".jpg", result);
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("IMAGE/PNG", true)]
    [InlineData("image/gif", false)]
    [InlineData("application/pdf", false)]
    [InlineData(null, false)]
    public void ImageContentTypes_AllowsOnlySupportedImages(string? contentType, bool expected) =>
        Assert.Equal(expected, ImageContentTypes.IsAllowed(contentType));
}