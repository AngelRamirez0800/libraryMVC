namespace libraryMVC.Interfaces
{
    public interface IAuthorBookRepository
    {
        Task UpdateAsync(Guid authorId, IEnumerable<Guid> bookIds);
    }
}
