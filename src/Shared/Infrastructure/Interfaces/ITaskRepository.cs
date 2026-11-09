using MongoDB.Driver;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Interfaces;

public interface ITaskRepository
{
    Task<ImageTask> InsertAsync(ImageTask task, CancellationToken cancellationToken = default);
    Task<ImageTask> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(ImageTask task, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<PageList<ImageTask>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
