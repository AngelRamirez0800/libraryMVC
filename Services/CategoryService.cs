using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class CategoryService : Service<Category, Guid>, ICategoryService
    {
        private readonly ICategoryRespository _repository;
        private readonly ICategoryBookService _categoryBookService;

        public CategoryService(
            ICategoryRespository repository,
            ICategoryBookService categoryBookService) : base(repository, repository, repository)
        {
            _repository = repository;
            _categoryBookService = categoryBookService;
        }

        public async Task<Category> CreateAsync(CategoryInput input, IEnumerable<Guid>? bookIds)
        {
            await EnsureUniqueNameAsync(input.Name);

            var category = new Category();
            category.UpdateDetails(input.Name.Trim(), input.Description.Trim());
            if (!input.IsActive)
            {
                category.Deactivate();
            }

            category.MarkCreated();
            var createdCategory = await AddAsync(category);
            await _categoryBookService.UpdateAsync(createdCategory.Id, bookIds ?? Enumerable.Empty<Guid>());
            return createdCategory;
        }

        public async Task<Category?> UpdateAsync(Guid id, CategoryInput input, IEnumerable<Guid> bookIds)
        {
            var existingCategory = await GetByIdAsync(id);
            if (existingCategory == null)
            {
                return null;
            }

            await EnsureUniqueNameAsync(input.Name, id);
            existingCategory.UpdateDetails(input.Name.Trim(), input.Description.Trim());
            if (input.IsActive)
            {
                existingCategory.Activate();
            }
            else
            {
                existingCategory.Deactivate();
            }

            var updatedCategory = await base.UpdateAsync(existingCategory);
            await _categoryBookService.UpdateAsync(id, bookIds);
            return updatedCategory;
        }

        public async Task<Category?> GetByIdWithBooksAsync(Guid id)
        {
            return await _repository.GetByIdWithBooksAsync(id);
        }

        public new Task<IList<Category?>> GetAllActiveAsync()
        {
            return base.GetAllActiveAsync();
        }

        public new async Task<bool> DeleteAsync(Guid id)
        {
            if (await _repository.HasBooksAsync(id))
            {
                return false;
            }

            return await base.DeleteAsync(id);
        }

        private async Task EnsureUniqueNameAsync(string name, Guid? excludingId = null)
        {
            if (await _repository.NameExistsAsync(name, excludingId))
            {
                throw new InvalidOperationException("Ya existe una categoría con ese nombre.");
            }
        }
    }
}
