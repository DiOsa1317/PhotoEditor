using ImageMagick;
using Worker.Domain;
using Worker.Infrastructure.Magick;
using FilterType = Worker.Domain.FilterType;

namespace UnitTests;

public class MagickImageProcessorTests
{
    private const ushort Max = 65535;
    private const int ImageSize = 8;

    [TestCase(FilterType.Negative)]
    [TestCase(FilterType.Mirror)]
    [TestCase(FilterType.Blur)]
    [TestCase(FilterType.Sharpen)]
    public async Task Process_KeepsImageSize(FilterType filterType)
    {
        var result = await Process(StepImage(), new FilterRequest(filterType, BlurRadius: 2));
        using var image = new MagickImage(result);

        Assert.Multiple(() =>
        {
            Assert.That(image.Width, Is.EqualTo(ImageSize));
            Assert.That(image.Height, Is.EqualTo(ImageSize));
        });
    }

    [TestCase(FilterType.Negative)]
    [TestCase(FilterType.Mirror)]
    public async Task StepHalves_AreSwapped(FilterType filterType)
    {
        var result = await Process(StepImage(), new FilterRequest(filterType));

        Assert.Multiple(() =>
        {
            Assert.That(ChannelAt(result, 0, 0), Is.EqualTo(Max),
                "Левая половина должна стать белой");
            Assert.That(ChannelAt(result, 7, 0), Is.EqualTo(0),
                "Правая половина должна стать чёрной");
        });
    }

    [Test]
    public async Task Blur_SoftensStepEdge()
    {
        var result = await Process(StepImage(), new FilterRequest(FilterType.Blur, BlurRadius: 2));

        Assert.Multiple(() =>
        {
            Assert.That(ChannelAt(result, 3, 0), Is.GreaterThan(0).And.LessThan(Max),
                "Край черной половины должен посветлеть");
            Assert.That(ChannelAt(result, 4, 0),  Is.GreaterThan(0).And.LessThan(Max),
                "Край белой половины должен затемниться");
        });
    }

    [Test]
    public async Task Sharpen_ChangesInteriorOfCurvedGradient()
    {
        var source = RampMessage();
        var result = await Process(source, new  FilterRequest(FilterType.Sharpen));
        
        Assert.That(ChannelAt(result, 4, 4), Is.Not.EqualTo(ChannelAt(source, 4, 4)),
            "На параболическом градиенте unsharp-маска обязана изменить центр");
    }
    
    private static Task<byte[]> Process(byte[] image, FilterRequest filterRequest) =>
    new  MagickImageProcessor().ProcessAsync(image, filterRequest, CancellationToken.None);

    private static byte[] StepImage() => CreateImage((x, _) => x < 4 ? (ushort)0 : Max);

    private static byte[] RampMessage() =>
        CreateImage((x, y) => (ushort)((x * x + y * y) * Max / 98));

    private static byte[] CreateImage(Func<int, int, ushort> channel)
    {
        using var image = new MagickImage(MagickColors.Black, ImageSize, ImageSize);
        using (var pixels = image.GetPixels())
        {
            for (var y = 0; y < ImageSize; y++)
            {
                for (var x = 0; x < ImageSize; x++)
                {
                    var value = channel(x, y);
                    pixels.SetPixel(x, y, [value, value, value]);
                }
            }
        }
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    private static ushort ChannelAt(byte[] image, int x, int y)
    {
        using var magick = new MagickImage(image);
        using var pixels = magick.GetPixels();
        return pixels.GetPixel(x, y).GetChannel(0);
    }
}