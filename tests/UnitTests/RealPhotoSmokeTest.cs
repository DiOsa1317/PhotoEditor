using Worker.Domain;
using Worker.Infrastructure.Magick;

namespace UnitTests;

[TestFixture, Explicit("Ручная визуальная проверка на реальной фотке")]
public class RealPhotoSmokeTest
{
    [Test]
    public void ApplyAllFilters_ToSamplePhoto()
    {
        var sourcePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "sample.jpg");
        var source = File.ReadAllBytes(sourcePath);
        var processor = new MagickImageProcessor();

        foreach (var filter in Enum.GetValues<FilterType>())
        {
            var result = processor
                .ProcessAsync(source, new FilterRequest(filter, BlurRadius: 5),
                    CancellationToken.None)
                .Result;
            var outPath  = Path.Combine(TestContext.CurrentContext.TestDirectory, $"{filter}.jpg");
            File.WriteAllBytes(outPath, result);
            TestContext.Out.WriteLine($"{filter} -> {outPath}");
        }
    }
}