using System.Net.Sockets;
using PhotoGallery.Core;

namespace PhotoGallery.Tests.Integration;

internal static class LocalStack
{
    public const string Endpoint = "http://127.0.0.1:4566";

    public static readonly GalleryOptions Options = new("PhotoGallery", "photo-gallery-local", Endpoint);

    public static readonly bool IsRunning = CheckIsRunning();

    private static bool CheckIsRunning()
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync("127.0.0.1", 4566).Wait(TimeSpan.FromMilliseconds(500)) && client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}