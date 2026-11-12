namespace libraryMVC.Interfaces;

public sealed record LiteratureSeedResult(
    int Authors,
    int Books,
    int Categories,
    int BookImages);

public interface ILiteratureSeedService
{
    Task<LiteratureSeedResult> SeedAsync(CancellationToken cancellationToken = default);
}
