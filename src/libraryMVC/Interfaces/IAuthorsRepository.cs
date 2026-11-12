using libraryMVC.DTOs.Lists;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorsRepository :
        IEntityReaderRepository<Author, Guid>,
        IEntityWriterRepository<Author, Guid>,
        IEntityLifecycleRepository<Author, Guid>
    {
        Task<Author?> GetByIdWithBooksAsync(Guid id);
        Task<PageSlice<AuthorListItem>> GetPageAsync(
            AuthorListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetNationalitiesAsync(CancellationToken cancellationToken = default);
    }
}
