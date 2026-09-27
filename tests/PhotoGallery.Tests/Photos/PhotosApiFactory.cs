using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PhotoGallery.Core;
using PhotoGallery.Core.Persistence;
using PhotoGallery.Core.Storage;
using PhotoGallery.Photos.Auth;
using PhotoGallery.Tests.Upload;

namespace PhotoGallery.Tests.Photos;

public sealed class PhotosApiFactory : WebApplicationFactory<Program>
{
    internal InMemoryGalleryRepository Repository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(new GalleryOptions("table", "bucket", "http://localstack:4566"));
            services.AddSingleton<IGalleryRepository>(Repository);
            services.AddSingleton<IUrlSigner, FakeUrlSigner>();
        });

    public HttpClient CreateClientFor(string userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(GalleryAuthenticationHandler.DebugUserHeader, userId);
        return client;
    }
}