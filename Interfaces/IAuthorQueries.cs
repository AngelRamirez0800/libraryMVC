using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorQueries
    {
        Task<IList<Author?>> GetAllAsync();
        Task<Author?> GetByIdWithBooksAsync(Guid id);
    }
}
