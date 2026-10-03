namespace PhotoGallery.Core;

/// <summary>Shared settings for Powertools logging, metrics and tracing across the Lambda functions.</summary>
public static class Observability
{
    /// <summary>CloudWatch namespace for the custom metrics every function emits.</summary>
    public const string MetricsNamespace = "PhotoGallery";

    /// <summary>True inside the Lambda runtime (including <c>sam local</c>).</summary>
    public static bool IsRunningInLambda =>
        Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME") is not null;

    /// <summary>
    /// X-Ray instrumentation of the AWS SDK is only registered in real AWS: tests, local runs and
    /// <c>sam local</c> against LocalStack have no X-Ray daemon or trace segment to attach SDK calls to.
    /// </summary>
    public static bool IsTracingEnabled =>
        IsRunningInLambda
        && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOCALSTACK_ENDPOINT"))
        && Environment.GetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED") != "true";
}
