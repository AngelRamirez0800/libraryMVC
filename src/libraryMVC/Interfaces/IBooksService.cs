using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

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
