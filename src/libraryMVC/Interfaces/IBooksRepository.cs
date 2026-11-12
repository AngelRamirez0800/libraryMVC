using libraryMVC.DTOs.Lists;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBooksRepository :
        IEntityReaderRepository<Book, Guid>,
        IEntityWriterRepository<Book, Guid>,
        IEntityLifecycleRepository<Book, Guid>
    {
        Task<Book?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Book>> GetRandomFeaturedBooksAsync(int count = 3);
        Task<PageSlice<BookListItem>> GetPageAsync(
            BookListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<BookFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    }
}
