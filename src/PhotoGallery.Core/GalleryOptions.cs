namespace PhotoGallery.Core;

public sealed record GalleryOptions(string TableName, string BucketName, string? LocalStackEndpoint, string? S3PublicEndpoint = null)
{
    public bool UseLocalStack => !string.IsNullOrWhiteSpace(LocalStackEndpoint);

    public string? PresignEndpoint =>
        string.IsNullOrWhiteSpace(S3PublicEndpoint) ? LocalStackEndpoint : S3PublicEndpoint;

    public static GalleryOptions FromEnvironment() => new(
        Required("TABLE_NAME"),
        Required("BUCKET_NAME"),
        Environment.GetEnvironmentVariable("LOCALSTACK_ENDPOINT"),
        Environment.GetEnvironmentVariable("S3_PUBLIC_ENDPOINT"));

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Environment variable '{name}' is not set.");
}