using Microsoft.AspNetCore.Http;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBookImageService
    {
        Task AddToBookAsync(Guid bookId, ICollection<IFormFile> imageFiles);
        Task RemoveAsync(Guid imageId);
    }
}
