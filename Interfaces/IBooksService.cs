using Microsoft.AspNetCore.Http;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBooksService : IService<Book, Guid>
    {
        Task<Book?> GetByIdWithImagesAsync(Guid id);
        Task<Book?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Book>> GetFeaturedBooksAsync(int count = 3);
    }
}
