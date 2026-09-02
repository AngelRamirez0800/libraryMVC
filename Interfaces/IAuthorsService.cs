using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorsService : IService<Author, Guid>
    {
        Task<Author> CreateAsync(Author author, IEnumerable<Guid>? bookIds);
        Task<Author?> UpdateAsync(Guid id, Author author, IEnumerable<Guid> bookIds);
        Task<Author?> GetByIdWithBooksAsync(Guid id);
    }
}
