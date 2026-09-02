using Microsoft.AspNetCore.Http;
using libraryMVC.Models;

namespace libraryMVC.Interfaces
{
    public interface IBooksService :
        IEntityReaderService<Book, Guid>,
        IEntityWriterService<Book, Guid>,
        IEntityLifecycleService<Book, Guid>,
        IBookQueries,
        IBookLifecycle
    {
    }
}
