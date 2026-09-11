using libraryMVC.Interfaces;

namespace libraryMVC.Services
{
    public class CategoryBookService : ICategoryBookService
    {
        private readonly ICategoryBookRepository _repository;

        public CategoryBookService(ICategoryBookRepository repository)
        {
            _repository = repository;
        }

        public Task UpdateAsync(Guid categoryId, IEnumerable<Guid> bookIds)
        {
            return _repository.UpdateAsync(categoryId, bookIds);
        }

        public Task UpdateForBookAsync(Guid bookId, IEnumerable<Guid> categoryIds)
        {
            return _repository.UpdateForBookAsync(bookId, categoryIds);
        }
    }
}
