// Local development only: stands in for the S3 -> Lambda trigger that exists on AWS.
// LocalStack sends S3 "ObjectCreated" notifications to an SQS queue; this loop hands them to ProcessingFunction.
using System.Text;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.TestUtilities;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using PhotoGallery.Core;
using PhotoGallery.Processing;

var options = GalleryOptions.FromEnvironment();
if (!options.UseLocalStack)
    throw new InvalidOperationException("LocalProcessor only runs against LocalStack (set LOCALSTACK_ENDPOINT).");

var queueName = Environment.GetEnvironmentVariable("PROCESSING_QUEUE_NAME") ?? "photo-gallery-processing";
var sqs = new AmazonSQSClient(
    new BasicAWSCredentials("test", "test"),
    new AmazonSQSConfig
    {
        ServiceURL = options.LocalStackEndpoint,
        AuthenticationRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-central-1"
    });

var function = new ProcessingFunction();
var serializer = new DefaultLambdaJsonSerializer();
var retryDelay = TimeSpan.FromSeconds(5);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Waiting for S3 upload events on '{queueName}'. Press Ctrl+C to stop.");

string? queueUrl = null;

while (!cts.IsCancellationRequested)
{
    ReceiveMessageResponse response;
    try
    {
        // Resolved lazily so the tool survives LocalStack starting late or being recreated.
        queueUrl ??= (await sqs.GetQueueUrlAsync(queueName, cts.Token)).QueueUrl;
        response = await sqs.ReceiveMessageAsync(
            new ReceiveMessageRequest { QueueUrl = queueUrl, MaxNumberOfMessages = 10, WaitTimeSeconds = 10 },
            cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"LocalStack not reachable ({ex.GetType().Name}), retrying in {retryDelay.TotalSeconds:0} s…");
        queueUrl = null;
        try
        {
            await Task.Delay(retryDelay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        continue;
    }

    foreach (var message in response.Messages ?? [])
    {
        try
        {
            using var body = new MemoryStream(Encoding.UTF8.GetBytes(message.Body));
            var s3Event = serializer.Deserialize<S3Event>(body);

            // S3 sends an "s3:TestEvent" without records when the notification is configured.
            if (s3Event.Records is { Count: > 0 })
                await function.HandleAsync(s3Event, new TestLambdaContext());

            await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Leave the message on the queue; it becomes visible again and is retried.
            Console.Error.WriteLine($"Processing failed: {ex.Message}");
        }
    }
}
