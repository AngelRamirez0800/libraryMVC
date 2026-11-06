using libraryMVC.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace libraryMVC.Tests.Unit.Services;

public sealed class ImageFileValidatorTests
{
    private readonly ImageFileValidator _validator = new();

    [Theory]
    [InlineData("cover.webp")]
    [InlineData("cover.jpg")]
    [InlineData("cover.jpeg")]
    [InlineData("cover.png")]
    [InlineData("COVER.PNG")]
    public void ValidateAcceptsSupportedExtensions(string fileName)
    {
        using var content = new MemoryStream(new byte[] { 1 });
        var file = new FormFile(content, 0, content.Length, "image", fileName);

        _validator.Validate(file);
    }

    [Fact]
    public void ValidateRejectsEmptyFile()
    {
        using var content = new MemoryStream();
        var file = new FormFile(content, 0, 0, "image", "cover.png");

        Assert.Throws<ArgumentException>(() => _validator.Validate(file));
    }

    [Fact]
    public void ValidateRejectsUnsupportedExtension()
    {
        using var content = new MemoryStream(new byte[] { 1 });
        var file = new FormFile(content, 0, content.Length, "image", "cover.gif");

        Assert.Throws<ArgumentException>(() => _validator.Validate(file));
    }

    [Fact]
    public void ValidateRejectsFilesLargerThanFiveMegabytes()
    {
        const int fileSize = 5 * 1024 * 1024 + 1;
        using var content = new MemoryStream(new byte[fileSize]);
        var file = new FormFile(content, 0, content.Length, "image", "cover.png");

        Assert.Throws<ArgumentException>(() => _validator.Validate(file));
    }
}
