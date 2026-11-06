using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorsRepository :
        IEntityReaderRepository<Author, Guid>,
        IEntityWriterRepository<Author, Guid>,
        IEntityLifecycleRepository<Author, Guid>
    {
        Task<Author?> GetByIdWithBooksAsync(Guid id);
    }
}
