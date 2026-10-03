using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using PhotoGallery.Processing;
using PhotoGallery.Upload;

namespace PhotoGallery.Tests;

/// <summary>
/// The functions use source-generated serializers instead of reflection. These tests feed them event payloads
/// shaped like the ones AWS sends, so a missing [JsonSerializable] or a naming mismatch fails here, not in Lambda.
/// </summary>
public class LambdaSerializationTests
{
    [Fact]
    public void S3Event_IsDeserialized()
    {
        const string json = """
            {
              "Records": [{
                "eventName": "ObjectCreated:Post",
                "s3": {
                  "bucket": { "name": "photos" },
                  "object": { "key": "originals/u1/a1/p1/my+photo.jpg", "size": 1024 }
                }
              }]
            }
            """;

        var s3Event = Deserialize<S3Event, ProcessingJsonContext>(json);

        var entity = Assert.Single(s3Event.Records).S3.Object;
        Assert.Equal("originals/u1/a1/p1/my photo.jpg", entity.KeyDecoded);
        Assert.Equal(1024, entity.Size);
    }

    [Fact]
    public void DynamoDbTtlRemoveEvent_IsDeserialized()
    {
        const string json = """
            {
              "Records": [{
                "eventName": "REMOVE",
                "userIdentity": { "type": "Service", "principalId": "dynamodb.amazonaws.com" },
                "dynamodb": {
                  "OldImage": {
                    "EntityType": { "S": "Photo" },
                    "Status": { "S": "Pending" },
                    "PhotoId": { "S": "p1" }
                  }
                }
              }]
            }
            """;

        var dynamoEvent = Deserialize<DynamoDBEvent, ProcessingJsonContext>(json);

        var record = Assert.Single(dynamoEvent.Records);
        Assert.True(ExpiredUploadCleanupFunction.IsTtlExpiry(record));
        Assert.Equal("p1", record.Dynamodb.OldImage["PhotoId"].S);
    }

    [Fact]
    public void HttpApiRequest_IsDeserialized()
    {
        const string json = """
            {
              "version": "2.0",
              "rawPath": "/api/photos/upload-url",
              "headers": { "content-type": "application/json" },
              "requestContext": {
                "http": { "method": "POST" },
                "authorizer": { "jwt": { "claims": { "sub": "user-1" } } }
              },
              "body": "{\"fileName\":\"a.jpg\"}",
              "isBase64Encoded": false
            }
            """;

        var request = Deserialize<APIGatewayHttpApiV2ProxyRequest, UploadLambdaJsonContext>(json);

        Assert.Equal("POST", request.RequestContext.Http.Method);
        Assert.Equal("user-1", request.RequestContext.Authorizer.Jwt.Claims["sub"]);
        Assert.Equal("{\"fileName\":\"a.jpg\"}", request.Body);
    }

    [Fact]
    public void HttpApiResponse_IsSerializedWithLambdaNames()
    {
        var response = HttpResults.BadRequest("Nope.");

        var serializer = new SourceGeneratorLambdaJsonSerializer<UploadLambdaJsonContext>();
        using var stream = new MemoryStream();
        serializer.Serialize(response, stream);
        using var json = System.Text.Json.JsonDocument.Parse(stream.ToArray());

        Assert.Equal(400, json.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("""{"error":"Nope."}""", json.RootElement.GetProperty("body").GetString());
    }

    private static T Deserialize<T, TContext>(string json)
        where TContext : System.Text.Json.Serialization.JsonSerializerContext
    {
        var serializer = new SourceGeneratorLambdaJsonSerializer<TContext>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return serializer.Deserialize<T>(stream);
    }
}
