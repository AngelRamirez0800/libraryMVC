using Microsoft.AspNetCore.Http;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBookImageService
    {
        Task<BookImage> SaveBookImageAsync(Guid bookId, IFormFile imageFile);
        void DeleteBookImage(BookImage bookImage);
    }
}
