using libraryMVC.Abstractions;

namespace libraryMVC.Interfaces
{
    public interface IEntityReaderRepository<T, I> where T : IEntity<I>
    {
        Task<T?> GetByIdAsync(I id);
        Task<IList<T?>> GetAllAsync();
        Task<IList<T?>> GetAllActiveAsync();
        Task<IList<T?>> GetAllInactiveAsync();
    }
}
