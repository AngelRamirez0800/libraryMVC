using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBooksRepository : IRepository<Book, Guid>
    {
        Task<Book?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Book>> GetRandomFeaturedBooksAsync(int count = 3);
    }
}
