using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBookQueries
    {
        Task<IList<Book?>> GetAllAsync();
        Task<Book?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Book>> GetFeaturedBooksAsync(int count = 3);
    }
}
