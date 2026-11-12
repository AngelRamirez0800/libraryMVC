using libraryMVC.DTOs.Lists;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface ICategoryRespository :
        IEntityReaderRepository<Category, Guid>,
        IEntityWriterRepository<Category, Guid>,
        IEntityLifecycleRepository<Category, Guid>
    {
        Task<Category?> GetByIdWithBooksAsync(Guid id);
        Task<PageSlice<CategoryListItem>> GetPageAsync(
            CategoryListFilter filters,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<bool> HasBooksAsync(Guid id);
        Task<bool> NameExistsAsync(string name, Guid? excludingId = null);
    }
}
