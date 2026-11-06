using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Interfaces
{
    public interface IBookImageService
    {
        Task AddToBookAsync(Guid bookId, ICollection<IFormFile> imageFiles);
        Task RemoveAsync(Guid imageId);
    }
}
