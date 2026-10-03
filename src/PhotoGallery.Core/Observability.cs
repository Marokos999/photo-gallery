namespace PhotoGallery.Core;

/// <summary>Shared settings for Powertools logging, metrics and tracing across the Lambda functions.</summary>
public static class Observability
{
    /// <summary>CloudWatch namespace for the custom metrics every function emits.</summary>
    public const string MetricsNamespace = "PhotoGallery";

    /// <summary>
    /// True inside the Lambda runtime. X-Ray instrumentation of the AWS SDK is only registered there:
    /// tests and local runs have no trace segment to attach SDK calls to.
    /// </summary>
    public static bool IsRunningInLambda =>
        Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME") is not null;
}
