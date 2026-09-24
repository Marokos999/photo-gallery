using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;

namespace PhotoGallery.Core;

public static class AwsClientFactory
{
    private static readonly AWSCredentials LocalCredentials = new BasicAWSCredentials("test", "test");

    private static string LocalRegion =>
        Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-central-1";

    public static IAmazonDynamoDB CreateDynamoDb(GalleryOptions options)
    {
        if (!options.UseLocalStack)
            return new AmazonDynamoDBClient();

        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = options.LocalStackEndpoint,
            AuthenticationRegion = LocalRegion
        };
        return new AmazonDynamoDBClient(LocalCredentials, config);
    }

    public static IAmazonS3 CreateS3(GalleryOptions options) =>
        options.UseLocalStack ? CreateLocalS3(options.LocalStackEndpoint!) : new AmazonS3Client();

    public static IAmazonS3 CreateS3Presigner(GalleryOptions options) =>
        options.UseLocalStack ? CreateLocalS3(options.PresignEndpoint!) : new AmazonS3Client();

    private static AmazonS3Client CreateLocalS3(string endpoint)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = LocalRegion,
            ForcePathStyle = true
        };
        return new AmazonS3Client(LocalCredentials, config);
    }
}