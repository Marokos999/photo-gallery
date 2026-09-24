using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;

namespace PhotoGallery.Core;

public sealed class AwsClientFactory
{
  private static readonly AWSCredentials LocalCredentials  = new BasicAWSCredentials("test", "test");

  private static string LocalRegion => Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-central-1";


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

  public static IAmazonS3 CreateS3(GalleryOptions options)
  {
    if(!options.UseLocalStack) return new AmazonS3Client();

    var config = new AmazonS3Config
    {
    ServiceURL = options.LocalStackEndpoint,
    AuthenticationRegion = LocalRegion,
    ForcePathStyle = true
    };

    return new AmazonS3Client(LocalCredentials, config);
  }
}