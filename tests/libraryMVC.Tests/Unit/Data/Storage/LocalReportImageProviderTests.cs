using libraryMVC.Data.Storage;
using libraryMVC.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Data.Storage;

public sealed class LocalReportImageProviderTests
{
    [Fact]
    public async Task ReadsFilesUnderWebRootAndReturnsNullForMissingImages()
    {
        string webRoot = Path.Combine(Path.GetTempPath(), $"report-images-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(webRoot, "images"));
        string imagePath = Path.Combine(webRoot, "images", "cover.jpg");
        byte[] expected = [1, 2, 3];
        await File.WriteAllBytesAsync(imagePath, expected);

        try
        {
            IReportImageProvider provider = CreateProvider(webRoot);

            Assert.Equal(expected, await provider.ReadAsync("images/cover.jpg"));
            Assert.Null(await provider.ReadAsync("images/missing.jpg"));
            await Assert.ThrowsAsync<InvalidDataException>(() => provider.ReadAsync("../outside.jpg"));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    private static IReportImageProvider CreateProvider(string webRoot)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.WebRootPath).Returns(webRoot);
        return new LocalReportImageProvider(environment.Object);
    }
}
