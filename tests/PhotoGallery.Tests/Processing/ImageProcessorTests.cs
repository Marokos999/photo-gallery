using PhotoGallery.Processing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace PhotoGallery.Tests.Processing;

public class ImageProcessorTests
{
    private readonly ImageProcessor _processor = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LargeImage_ProducesWebpVariants_WithExpectedSizes()
    {
        using var input = TestImages.CreateJpeg(3000, 1500);

        var result = await _processor.ProcessAsync(input, Ct);

        Assert.Equal((3000, 1500), (result.Width, result.Height));
        using var thumbnail = Image.Load(result.Thumbnail);
        using var preview = Image.Load(result.Preview);
        Assert.Equal((400, 200), (thumbnail.Width, thumbnail.Height));
        Assert.Equal((1200, 600), (preview.Width, preview.Height));
        Assert.Equal("image/webp", Image.DetectFormat(result.Thumbnail).DefaultMimeType);
    }

    [Fact]
    public async Task SmallImage_IsNotUpscaled()
    {
        using var input = TestImages.CreateJpeg(300, 150);

        var result = await _processor.ProcessAsync(input, Ct);

        using var thumbnail = Image.Load(result.Thumbnail);
        Assert.Equal(300, thumbnail.Width);
    }

    [Fact]
    public async Task GpsExif_IsStripped()
    {
        using var input = TestImages.CreateJpeg(800, 400, exif =>
            exif.SetValue(ExifTag.GPSLatitude, [new Rational(44, 1), new Rational(49, 1), new Rational(0, 1)]));

        var result = await _processor.ProcessAsync(input, Ct);

        using var thumbnail = Image.Load(result.Thumbnail);
        Assert.Null(thumbnail.Metadata.ExifProfile);
    }

    [Fact]
    public async Task ExifOrientation_IsApplied()
    {
        using var input = TestImages.CreateJpeg(300, 200, exif => exif.SetValue(ExifTag.Orientation, (ushort)6));

        var result = await _processor.ProcessAsync(input, Ct);

        Assert.Equal((200, 300), (result.Width, result.Height));
    }

    [Fact]
    public async Task InvalidImage_Throws()
    {
        using var input = new MemoryStream([1, 2, 3, 4]);

        await Assert.ThrowsAnyAsync<ImageFormatException>(() => _processor.ProcessAsync(input, Ct));
    }
}