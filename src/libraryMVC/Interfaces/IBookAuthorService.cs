namespace libraryMVC.Interfaces
{
    public interface IBookAuthorService
    {
        Task UpdateAsync(Guid bookId, IEnumerable<Guid> authorIds);
    }
}
