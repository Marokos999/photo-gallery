namespace PhotoGallery.Core.Models;

public sealed record ProcessedImage(string ThumbnailKey, string PreviewKey, int Width, int Height);