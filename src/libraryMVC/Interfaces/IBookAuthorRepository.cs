namespace libraryMVC.Interfaces
{
    public interface IBookAuthorRepository
    {
        Task UpdateAsync(Guid bookId, IEnumerable<Guid> authorIds);
    }
}
