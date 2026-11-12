using libraryMVC.DTOs.Lists;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorQueries
    {
        Task<IList<Author?>> GetAllAsync();
        Task<Author?> GetByIdWithBooksAsync(Guid id);
        Task<PageSlice<AuthorListItem>> GetPageAsync(
            AuthorListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetNationalitiesAsync(CancellationToken cancellationToken = default);
    }
}
