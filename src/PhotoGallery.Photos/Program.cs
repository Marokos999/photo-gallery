using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using PhotoGallery.Core;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;
using PhotoGallery.Photos.Albums;
using PhotoGallery.Photos.Auth;
using PhotoGallery.Photos.Photos;
using PhotoGallery.Photos.Shares;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(_ => GalleryOptions.FromEnvironment());
builder.Services.AddSingleton(sp => AwsClientFactory.CreateDynamoDb(sp.GetRequiredService<GalleryOptions>()));
builder.Services.AddSingleton<IGalleryRepository, DynamoGalleryRepository>();
builder.Services.AddSingleton<IUrlSigner>(sp =>
{
    var options = sp.GetRequiredService<GalleryOptions>();
    return new S3UrlSigner(AwsClientFactory.CreateS3Presigner(options), options);
});
builder.Services.AddSingleton(sp => AwsClientFactory.CreateS3(sp.GetRequiredService<GalleryOptions>()));
builder.Services.AddSingleton<IPhotoStorage, S3PhotoStorage>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAuthentication(GalleryAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, GalleryAuthenticationHandler>
                (GalleryAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api").RequireAuthorization();
api.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).AllowAnonymous();
api.MapAlbumEndpoints();
api.MapPhotoEndpoints();
api.MapShareEndpoints();

app.Run();

public partial class Program;