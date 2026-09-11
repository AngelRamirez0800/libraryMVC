using libraryMVC.DTOs;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface ICategoryApplicationService
    {
        Task<Category> CreateAsync(CategoryInput input, IEnumerable<Guid>? bookIds);
        Task<Category?> UpdateAsync(Guid id, CategoryInput input, IEnumerable<Guid> bookIds);
    }
}
