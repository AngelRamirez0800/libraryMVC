using libraryMVC.DTOs.Lists;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface ICategoryQueries
    {
        Task<IList<Category?>> GetAllAsync();
        Task<IList<Category?>> GetAllActiveAsync();
        Task<Category?> GetByIdWithBooksAsync(Guid id);
        Task<PageSlice<CategoryListItem>> GetPageAsync(
            CategoryListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
