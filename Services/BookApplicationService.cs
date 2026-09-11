using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Services
{
    public class BookApplicationService : IBookApplicationService
    {
        private readonly IBooksService _books;
        private readonly IBookAuthorService _authors;
        private readonly ICategoryBookService _categories;
        private readonly ICategoryQueries _categoryQueries;
        private readonly IBookImageService _images;

        public BookApplicationService(
            IBooksService books,
            IBookAuthorService authors,
            ICategoryBookService categories,
            ICategoryQueries categoryQueries,
            IBookImageService images)
        {
            _books = books;
            _authors = authors;
            _categories = categories;
            _categoryQueries = categoryQueries;
            _images = images;
        }

        public async Task<Book> CreateAsync(
            BookInput input,
            IEnumerable<Guid>? authorIds,
            IEnumerable<Guid>? categoryIds,
            ICollection<IFormFile>? images)
        {
            var selectedCategoryIds = await ValidateCategoryIdsAsync(categoryIds);
            var book = new Book(input.Isbn, input.Title, input.Summary, input.IsActive);
            book.MarkCreated();
            await _books.AddAsync(book);
            await _authors.UpdateAsync(book.Id, authorIds ?? Enumerable.Empty<Guid>());
            await _categories.UpdateForBookAsync(book.Id, selectedCategoryIds);

            if (images != null && images.Count > 0)
            {
                await _images.AddToBookAsync(book.Id, images);
            }

            return book;
        }

        public async Task<Book?> UpdateAsync(
            Guid id,
            BookInput input,
            IEnumerable<Guid> authorIds,
            IEnumerable<Guid> categoryIds,
            ICollection<IFormFile>? images)
        {
            var selectedCategoryIds = await ValidateCategoryIdsAsync(categoryIds);
            var existingBook = await _books.GetByIdAsync(id);
            if (existingBook == null)
            {
                return null;
            }

            existingBook.UpdateDetails(input.Isbn, input.Title, input.Summary);

            if (input.IsActive)
            {
                existingBook.Activate();
            }
            else
            {
                existingBook.Deactivate();
            }

            await _books.UpdateAsync(existingBook);
            await _authors.UpdateAsync(id, authorIds);
            await _categories.UpdateForBookAsync(id, selectedCategoryIds);

            if (images != null && images.Count > 0)
            {
                await _images.AddToBookAsync(id, images);
            }

            return existingBook;
        }

        public Task AddImagesAsync(Guid bookId, ICollection<IFormFile> images)
        {
            return _images.AddToBookAsync(bookId, images);
        }

        public Task RemoveImageAsync(Guid imageId)
        {
            return _images.RemoveAsync(imageId);
        }

        private async Task<List<Guid>> ValidateCategoryIdsAsync(IEnumerable<Guid>? categoryIds)
        {
            var selectedCategoryIds = (categoryIds ?? Enumerable.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (selectedCategoryIds.Count == 0)
            {
                throw new ArgumentException("Debes seleccionar al menos una categoría.");
            }

            var activeCategories = await _categoryQueries.GetAllActiveAsync();
            var activeCategoryIds = activeCategories
                .Where(category => category != null)
                .Select(category => category!.Id)
                .ToHashSet();

            if (selectedCategoryIds.Any(id => !activeCategoryIds.Contains(id)))
            {
                throw new ArgumentException("Solo puedes seleccionar categorías activas existentes.");
            }

            return selectedCategoryIds;
        }
    }
}
