
namespace Shared.Infrastructure.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> GetAsync(string fileId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileId, CancellationToken cancellationToken = default);
}