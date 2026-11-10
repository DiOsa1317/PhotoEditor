namespace Shared.Domain;

public class ImageTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = Guid.NewGuid().ToString();
    public FilterType Filter { get; set; } = FilterType.Negative;
    public double BlurRadius { get; set; } = 0.0;
    public ImageTaskStatus Status { get; set; } = ImageTaskStatus.Pending;
    public int Attempts { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string LeaseOwner { get; set; } = string.Empty;
    public DateTime LeaseUntil { get; set; } = DateTime.UtcNow.AddMinutes(5);
    public string SourceFileId { get; set; } = string.Empty;
    public string ResultFileId { get; set; } = string.Empty;
    public string? Error { get; set; }
}
