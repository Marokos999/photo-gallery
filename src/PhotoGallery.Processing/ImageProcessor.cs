using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PhotoGallery.Processing;

public sealed record ProcessedVariants(int Width, int Height, byte[] Thumbnail, byte[] Preview);

public sealed class ImageProcessor
{
    public const int ThumbnailWidth = 400;
    public const int PreviewWidth = 1200;

    private static readonly WebpEncoder Encoder = new WebpEncoder { Quality = 80 };

    public async Task<ProcessedVariants> ProcessAsync(Stream input, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(input, ct);

        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;

        var thumbnail = await EncodeAsync(image, ThumbnailWidth, ct);
        var preview = await EncodeAsync(image, PreviewWidth, ct);

        return new ProcessedVariants(image.Width, image.Height, thumbnail, preview);
    }

    private static async Task<byte[]> EncodeAsync(Image source, int maxWidth, CancellationToken ct = default)
    {
        using var output = new MemoryStream();
        if (source.Width <= maxWidth) await source.SaveAsync(output, Encoder, ct);
        else
        {
            using var resized = source.Clone(x => x.Resize(maxWidth, 0));
            await resized.SaveAsync(output, Encoder, ct);
        }

        return output.ToArray();
    }

}