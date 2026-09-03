using libraryMVC.Abstractions;

namespace libraryMVC.Interfaces
{
    public interface IEntityLifecycleRepository<T, I> where T : IEntity<I>
    {
        Task<bool> DeleteAsync(I id);
        Task<bool> RestoreAsync(I id);
        Task<bool> AlreadyExistsAsync(I id);
    }
}
