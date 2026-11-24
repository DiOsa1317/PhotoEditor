using Shared.Domain;

namespace Shared.Infrastructure.Interfaces;

public interface IPhotoRepository
{
    Photo? GetPhotoById(string photoId);
    bool PostPhoto(Photo photo);
    bool UpdatePhotoStatus(string photoId, string newStatus);
}