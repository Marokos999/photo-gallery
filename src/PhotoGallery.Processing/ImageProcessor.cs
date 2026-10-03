using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PhotoGallery.Processing;

public sealed record ProcessedVariants(int Width, int Height, byte[] Thumbnail, byte[] Preview);

public sealed class ImageProcessor
{
    public const int ThumbnailWidth = 400;
    public const int PreviewWidth = 1200;

    /// <summary>50 megapixels: well above any phone camera, far below what would exhaust Lambda memory.</summary>
    public const long DefaultMaxPixels = 50_000_000;

    private static readonly WebpEncoder Encoder = new WebpEncoder { Quality = 80 };

    private readonly long _maxPixels;

    public ImageProcessor(long maxPixels = DefaultMaxPixels) => _maxPixels = maxPixels;

    public async Task<ProcessedVariants> ProcessAsync(Stream input, CancellationToken ct = default)
    {
        // S3 response streams are forward-only; Identify + Load need to read the image twice.
        using var buffered = new MemoryStream();
        await input.CopyToAsync(buffered, ct);
        buffered.Position = 0;

        // Reads only the header: rejects decompression bombs before any pixel is allocated.
        var info = await Image.IdentifyAsync(buffered, ct);
        if ((long)info.Width * info.Height > _maxPixels)
            throw new ImageTooLargeException(info.Width, info.Height, _maxPixels);

        buffered.Position = 0;
        using var image = await Image.LoadAsync(buffered, ct);

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