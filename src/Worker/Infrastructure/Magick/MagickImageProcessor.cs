using ImageMagick;
using Worker.Domain;
using Worker.Infrastructure.Interfaces;
using FilterType = Worker.Domain.FilterType;

namespace Worker.Infrastructure.Magick;

public class MagickImageProcessor : IImageProcessor
{
    public Task<byte[]> ProcessAsync(byte[] source, FilterRequest filterRequest, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                using var image = new MagickImage(source);
                Apply(image, filterRequest);
                return image.ToByteArray();
            }
            catch (ImageProcessingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ImageProcessingException("Обработка изображения через ImageMagick не удалась", ex);
            }
        }, cancellationToken);
    }

    private void Apply(MagickImage image, FilterRequest filterRequest)
    {
        switch (filterRequest.FilterType)
        {
            case FilterType.Negative : image.Negate(); break;
            case FilterType.Mirror : image.Flop(); break;
            case FilterType.Blur:
                image.Blur(0, filterRequest.BlurRadius > 0 ? filterRequest.BlurRadius : 5);
                break;
            case FilterType.Sharpen: image.Sharpen(0, 1);
                break;
            default:
                throw new ImageProcessingException($"Неизвестный фильтр: {filterRequest.FilterType}");
        }
    }
}