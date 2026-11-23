using MongoDB.Driver;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Mongo;

public class MongoTaskRepository : ITaskRepository
{
    private readonly IMongoCollection<ImageTask> _tasks;

    public MongoTaskRepository(IMongoDatabase database)
    {
        _tasks = database.GetCollection<ImageTask>("tasks");
    }

    public async Task<ImageTask> InsertAsync(ImageTask task, CancellationToken cancellationToken = default)
    {
        await _tasks.InsertOneAsync(task, new InsertOneOptions(), cancellationToken);
        return task;
    }

    public Task<ImageTask> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _tasks.Find(t => t.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public Task UpdateAsync(ImageTask task, CancellationToken cancellationToken = default)
    {
        return _tasks.ReplaceOneAsync(Builders<ImageTask>.Filter.Eq(t => t.Id, task.Id), task, new ReplaceOptions(),
            cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return _tasks.DeleteOneAsync(Builders<ImageTask>.Filter.Eq(t => t.Id, id), new DeleteOptions(),
            cancellationToken);
    }

    public async Task<PageList<ImageTask>> GetPageAsync(int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var skip = (pageNumber - 1) * pageSize;
        var filter = Builders<ImageTask>.Filter.Empty;
        var tasks = await _tasks.Find(filter).SortByDescending(t => t.LastUpdated).Skip(skip).Limit(pageSize)
            .ToListAsync(cancellationToken);
        var total = await _tasks.CountDocumentsAsync(filter, new CountOptions(), cancellationToken);
        return new PageList<ImageTask>(tasks, (int)total, pageNumber, pageSize);
    }

    public async Task<List<ImageTask>> GetStuckTasksAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ImageTask>.Filter.And(
            Builders<ImageTask>.Filter.Eq(t => t.Status, ImageTaskStatus.Processing),
            Builders<ImageTask>.Filter.Lt(t => t.LeaseUntil, DateTime.UtcNow)
        );
        return await _tasks.Find(filter).ToListAsync(cancellationToken);
    }
}