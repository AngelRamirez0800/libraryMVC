namespace libraryMVC.Interfaces
{
    public interface ICategoryBookService
    {
        Task UpdateAsync(Guid categoryId, IEnumerable<Guid> bookIds);
        Task UpdateForBookAsync(Guid bookId, IEnumerable<Guid> categoryIds);
    }
}
