using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Mongo;

public class MongoFileStorage : IFileStorage
{
    private readonly IGridFSBucket _bucket;

    public MongoFileStorage(IMongoDatabase database)
    {
        _bucket = new GridFSBucket(database);
    }

    public Task<string> SaveAsync(Stream stream, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        var id = ObjectId.GenerateNewId();
        var options = new GridFSUploadOptions { Metadata = new BsonDocument("content_type", contentType) };
        return _bucket.UploadFromStreamAsync(id, fileName, stream, options, cancellationToken)
            .ContinueWith(_ => id.ToString(), cancellationToken);
    }

    public Task<GridFSDownloadStream<ObjectId>> GetAsync(string fileId, CancellationToken cancellationToken = default)
    {
        return _bucket.OpenDownloadStreamAsync(ObjectId.Parse(fileId), cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        return _bucket.DeleteAsync(ObjectId.Parse(fileId), cancellationToken);
    }
}