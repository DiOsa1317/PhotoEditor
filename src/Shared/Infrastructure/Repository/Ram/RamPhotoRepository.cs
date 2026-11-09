using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace Shared.Infrastructure.Repository.Ram;

public class RamPhotoRepository : IPhotoRepository
{
    public Dictionary<string, Photo> Storage { get; }

    public RamPhotoRepository()
    {
        Storage = new Dictionary<string, Photo>();
    }

    public Photo? GetPhotoById(string photoId)
    {
        return Storage.GetValueOrDefault(photoId);
    }

    public bool PostPhoto(Photo photo)
    {
        if (Storage.ContainsKey(photo.Id))
            return false;
        
        Storage[photo.Id] = photo;
        return true;
    }

    public bool UpdatePhotoStatus(string photoId, string newStatus)
    {
        if (!Storage.TryGetValue(photoId, out var photo))
            return false;

        photo.Status = newStatus;
        return true;
    }
}