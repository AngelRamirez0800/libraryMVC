namespace libraryMVC.Interfaces
{
    public interface ICategoryLifecycle
    {
        Task<bool> DeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
