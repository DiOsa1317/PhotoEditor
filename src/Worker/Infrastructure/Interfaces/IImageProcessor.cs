using Worker.Domain;

namespace Worker.Infrastructure.Interfaces;

public interface IImageProcessor
{
    Task<byte[]> ProcessAsync(byte[] source, FilterRequest filterRequest, CancellationToken cancellationToken);
}