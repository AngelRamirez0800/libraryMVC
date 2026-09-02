using libraryMVC.Models;
using libraryMVC.DTOs;

namespace libraryMVC.Interfaces
{
    public interface IAuthorsService : IService<Author, Guid>
    {
        Task<Author> CreateAsync(AuthorInput input, IEnumerable<Guid>? bookIds);
        Task<Author?> UpdateAsync(Guid id, AuthorInput input, IEnumerable<Guid> bookIds);
        Task<Author?> GetByIdWithBooksAsync(Guid id);
    }
}
