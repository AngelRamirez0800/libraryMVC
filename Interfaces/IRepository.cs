using libraryMVC.Abstractions;

namespace libraryMVC.Interfaces
{
    public interface IRepository<T, I> :
        IEntityReaderRepository<T, I>,
        IEntityWriterRepository<T, I>,
        IEntityLifecycleRepository<T, I>
        where T : IEntity<I>
    {
    }
}
