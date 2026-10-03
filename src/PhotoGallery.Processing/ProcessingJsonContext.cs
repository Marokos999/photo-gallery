using System.Text.Json.Serialization;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.S3Events;

namespace PhotoGallery.Processing;

/// <summary>Compile-time JSON metadata for the events both functions in this assembly receive.</summary>
[JsonSerializable(typeof(S3Event))]
[JsonSerializable(typeof(DynamoDBEvent))]
public sealed partial class ProcessingJsonContext : JsonSerializerContext;
