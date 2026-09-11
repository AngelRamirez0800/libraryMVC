using libraryMVC.DTOs;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface ICategoryService :
        IEntityReaderService<Category, Guid>,
        IEntityWriterService<Category, Guid>,
        IEntityLifecycleService<Category, Guid>,
        ICategoryQueries,
        ICategoryLifecycle,
        ICategoryApplicationService
    {
    }
}
