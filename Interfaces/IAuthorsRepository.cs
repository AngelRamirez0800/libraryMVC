using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorsRepository : IRepository<Author, Guid>
    {
        Task<Author?> GetByIdWithBooksAsync(Guid id);
        Task UpdateAuthorBooksAsync(Guid authorId, IEnumerable<Guid> bookIds);
    }
}
