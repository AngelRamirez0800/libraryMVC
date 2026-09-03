using libraryMVC.Abstractions;

namespace libraryMVC.Interfaces
{
    public interface IEntityWriterRepository<T, I> where T : IEntity<I>
    {
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
    }
}
