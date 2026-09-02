using Microsoft.AspNetCore.Http;
using libraryMVC.Models;
using libraryMVC.DTOs;

namespace libraryMVC.Interfaces
{
    public interface IBookApplicationService
    {
        Task<Book> CreateAsync(BookInput input, IEnumerable<Guid>? authorIds, ICollection<IFormFile>? images);
        Task<Book?> UpdateAsync(Guid id, BookInput input, IEnumerable<Guid> authorIds, ICollection<IFormFile>? images);
        Task AddImagesAsync(Guid bookId, ICollection<IFormFile> images);
        Task RemoveImageAsync(Guid imageId);
    }
}
