using libraryMVC.DTOs.Lists;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBookQueries
    {
        Task<IList<Book?>> GetAllAsync();
        Task<Book?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Book>> GetFeaturedBooksAsync(int count = 3);
        Task<PageSlice<BookListItem>> GetPageAsync(
            BookListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<BookFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    }
}
