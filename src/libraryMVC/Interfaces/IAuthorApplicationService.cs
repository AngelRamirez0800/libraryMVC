using libraryMVC.DTOs;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorApplicationService
    {
        Task<Author> CreateAsync(AuthorInput input, IEnumerable<Guid>? bookIds);
        Task<Author?> UpdateAsync(Guid id, AuthorInput input, IEnumerable<Guid> bookIds);
    }
}
