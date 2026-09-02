namespace libraryMVC.Interfaces
{
    public interface IAuthorBookService
    {
        Task UpdateAsync(Guid authorId, IEnumerable<Guid> bookIds);
    }
}
