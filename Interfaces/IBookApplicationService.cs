using Microsoft.AspNetCore.Http;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBookApplicationService
    {
        Task<Book> CreateAsync(Book book, IEnumerable<Guid>? authorIds, ICollection<IFormFile>? images);
        Task<Book?> UpdateAsync(Guid id, Book book, IEnumerable<Guid> authorIds, ICollection<IFormFile>? images);
        Task AddImagesAsync(Guid bookId, ICollection<IFormFile> images);
        Task RemoveImageAsync(Guid imageId);
    }
}
