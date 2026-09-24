namespace PhotoGallery.Core;

public sealed record GalleryOptions(string TableName, string BucketName, string? LocalStackEndpoint)
{
    public bool UseLocalStack => !string.IsNullOrWhiteSpace(LocalStackEndpoint);

    public static GalleryOptions FromEnvironment() => new(
        Required("TABLE_NAME"),
        Required("BUCKET_NAME"),
        Environment.GetEnvironmentVariable("LOCALSTACK_ENDPOINT"));

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Environment variable '{name}' is not set.");
}