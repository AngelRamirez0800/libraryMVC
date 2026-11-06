using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Interfaces
{
    public sealed record StoredBookImage(string ImagePath, int ImageNumber, long FileSize);

    public interface IBookImageStorage
    {
        Task<StoredBookImage> SaveAsync(Guid bookId, IFormFile imageFile);
        void Delete(BookImage bookImage);
    }
}
