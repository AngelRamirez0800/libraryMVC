using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface ICategoryRespository :
        IEntityReaderRepository<Category, Guid>,
        IEntityWriterRepository<Category, Guid>,
        IEntityLifecycleRepository<Category, Guid>
    {
        Task<Category?> GetByIdWithBooksAsync(Guid id);
        Task<bool> HasBooksAsync(Guid id);
        Task<bool> NameExistsAsync(string name, Guid? excludingId = null);
    }
}
