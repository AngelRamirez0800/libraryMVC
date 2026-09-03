using libraryMVC.DTOs;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IAuthorsService :
        IEntityReaderService<Author, Guid>,
        IEntityWriterService<Author, Guid>,
        IEntityLifecycleService<Author, Guid>,
        IAuthorQueries,
        IAuthorLifecycle,
        IAuthorApplicationService
    {
    }
}
