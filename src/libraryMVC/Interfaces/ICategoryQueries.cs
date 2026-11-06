using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface ICategoryQueries
    {
        Task<IList<Category?>> GetAllAsync();
        Task<IList<Category?>> GetAllActiveAsync();
        Task<Category?> GetByIdWithBooksAsync(Guid id);
    }
}
