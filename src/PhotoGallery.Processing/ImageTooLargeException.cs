namespace PhotoGallery.Processing;

public sealed class ImageTooLargeException(int width, int height, long maxPixels)
    : Exception($"Image is {width}x{height} ({(long)width * height:N0} px), the limit is {maxPixels:N0} px.");
