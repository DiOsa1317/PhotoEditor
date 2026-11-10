using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace Shared.Infrastructure.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType,
        CancellationToken cancellationToken = default);

    Task<GridFSDownloadStream<ObjectId>> GetAsync(string fileId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileId, CancellationToken cancellationToken = default);
}