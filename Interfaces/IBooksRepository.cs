using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBooksRepository : IRepository<Book, Guid>
    {
        Task<Book?> GetByIdWithImagesAsync(Guid id);
        Task<Book?> GetByIdWithDetailsAsync(Guid id);
        Task UpdateBookAuthorsAsync(Guid bookId, IEnumerable<Guid> authorIds);
        Task<BookImage?> GetBookImageByIdAsync(Guid imageId);
        Task RemoveBookImageAsync(BookImage bookImage);
        Task<List<Book>> GetRandomFeaturedBooksAsync(int count = 3);
        Task SaveChangesAsync();
    }
}
