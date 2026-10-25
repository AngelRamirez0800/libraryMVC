namespace libraryMVC.Interfaces
{
    public enum CategoryDeactivationResult
    {
        Deactivated,
        NotFound,
        AlreadyInactive,
        HasBooks,
        Failed
    }

    public interface ICategoryLifecycle
    {
        Task<bool> DeleteAsync(Guid id);
        Task<CategoryDeactivationResult> DeactivateAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
    }
}
