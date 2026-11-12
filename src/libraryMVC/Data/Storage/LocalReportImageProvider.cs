using libraryMVC.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace libraryMVC.Data.Storage;

public sealed class LocalReportImageProvider : IReportImageProvider
{
    private readonly IWebHostEnvironment _environment;

    public LocalReportImageProvider(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<byte[]?> ReadAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        string webRoot = Path.GetFullPath(_environment.WebRootPath);
        string fullPath = Path.GetFullPath(Path.Combine(
            webRoot,
            imagePath.Replace('/', Path.DirectorySeparatorChar)));
        string rootPrefix = webRoot.EndsWith(Path.DirectorySeparatorChar)
            ? webRoot
            : webRoot + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.StartsWith(rootPrefix, comparison))
        {
            throw new InvalidDataException("A report image path resolved outside the web root.");
        }

        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }
}
