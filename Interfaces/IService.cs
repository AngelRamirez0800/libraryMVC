using libraryMVC.Abstractions;

namespace libraryMVC.Interfaces
{
    public interface IService<T, I> :
        IEntityReaderService<T, I>,
        IEntityWriterService<T, I>,
        IEntityLifecycleService<T, I>
        where T : IEntity<I>
    {
    }
}
