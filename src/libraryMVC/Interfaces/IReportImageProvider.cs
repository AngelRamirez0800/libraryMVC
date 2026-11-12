namespace libraryMVC.Interfaces;

public interface IReportImageProvider
{
    Task<byte[]?> ReadAsync(string imagePath, CancellationToken cancellationToken = default);
}
