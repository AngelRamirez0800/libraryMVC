using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorsRepository : IRepository<Author, Guid>
    {
        Task<Author?> GetByIdWithBooksAsync(Guid id);
    }
}
