using libraryMVC.Interfaces;

namespace libraryMVC.Services
{
    public class BookAuthorService : IBookAuthorService
    {
        private readonly IBookAuthorRepository _repository;

        public BookAuthorService(IBookAuthorRepository repository)
        {
            _repository = repository;
        }

        public Task UpdateAsync(Guid bookId, IEnumerable<Guid> authorIds)
        {
            return _repository.UpdateAsync(bookId, authorIds);
        }
    }
}
