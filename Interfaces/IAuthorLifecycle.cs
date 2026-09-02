namespace libraryMVC.Interfaces
{
    public interface IAuthorLifecycle
    {
        Task<bool> DeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
