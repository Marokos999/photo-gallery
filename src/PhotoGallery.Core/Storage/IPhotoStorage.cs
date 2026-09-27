namespace PhotoGallery.Core.Storage;

public interface IPhotoStorage
{
    Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct = default);
}