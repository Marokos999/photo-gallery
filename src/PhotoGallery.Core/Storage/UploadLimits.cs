namespace PhotoGallery.Core.Storage;

public static class UploadLimits
{
    /// <summary>Largest original accepted. Enforced by S3 itself through the presigned POST policy.</summary>
    public const long MaxFileBytes = 25 * 1024 * 1024;
}
