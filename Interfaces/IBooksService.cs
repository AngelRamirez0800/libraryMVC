using Microsoft.AspNetCore.Http;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBooksService : IService<Book, Guid>
    {
        Task<Book> CreateAsync(Book book, IEnumerable<Guid>? authorIds, ICollection<IFormFile>? images);
        Task<Book?> UpdateAsync(Guid id, Book book, IEnumerable<Guid> authorIds, ICollection<IFormFile>? images);
        Task<Book?> GetByIdWithImagesAsync(Guid id);
        Task<Book?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Book>> GetFeaturedBooksAsync(int count = 3);
        Task UpdateBookAuthorsAsync(Guid bookId, IEnumerable<Guid> authorIds);
        Task<Book> AddImagesToBookAsync(Guid bookId, ICollection<IFormFile> imageFiles);
        Task RemoveImageFromBookAsync(Guid bookImageId);
    }
}
