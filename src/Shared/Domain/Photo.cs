namespace Shared.Domain;

public class Photo
{
    public string Id { get; set; }
    public string Status { get; set; }
    public byte[]? ImageData { get; set; }
    public byte[]? ResultData { get; set; }

    public Photo(string id, string status, byte[]? imageData = null)
    {
        Id = id;
        Status = status;
        ImageData = imageData;
    }
}