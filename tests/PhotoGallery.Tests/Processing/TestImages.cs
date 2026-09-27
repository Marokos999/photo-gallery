using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoGallery.Tests.Processing;

internal static class TestImages
{
    public static MemoryStream CreateJpeg(int width, int height, Action<ExifProfile>? configureExif = null)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(100, 149, 237));

        if (configureExif is not null)
        {
            var exif = new ExifProfile();
            configureExif(exif);
            image.Metadata.ExifProfile = exif;
        }


        var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        stream.Position = 0;
        return stream;
    }
}