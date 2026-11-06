using libraryMVC.Interfaces;

namespace libraryMVC.Services
{
    public class AuthorBookService : IAuthorBookService
    {
        private readonly IAuthorBookRepository _repository;

        public AuthorBookService(IAuthorBookRepository repository)
        {
            _repository = repository;
        }

        public Task UpdateAsync(Guid authorId, IEnumerable<Guid> bookIds)
        {
            return _repository.UpdateAsync(authorId, bookIds);
        }
    }
}
