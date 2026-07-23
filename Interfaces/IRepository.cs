using libraryMVC.Abstractions;
using System.Net;

namespace libraryMVC.Interfaces
{
    public interface IRepository<T,I> where T: IEntity<I>
    {
        Task<T> AddAsync(T entity);
        Task<T> UpdateSync(T Entity);
        Task<bool> DeleteAsync (I id);
        Task<bool> RestoreAsync(I id);
        Task<bool> AlreadyExistsAsync(I id);
        Task<T?> GetByIdAsync(I id);
        Task<IList<T?>> GetAllAsync();
        Task<IList<T?>> GetAllActiveAsync();
        Task<IList<T?>> GetAllInactiveAsync();
    }
}
