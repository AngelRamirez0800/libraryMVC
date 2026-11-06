using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBookImageRepository
    {
        Task<BookImage> AddAsync(BookImage image);
        Task<BookImage?> GetByIdAsync(Guid imageId);
        Task RemoveAsync(BookImage image);
    }
}
