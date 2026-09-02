using libraryMVC.Interfaces;
using libraryMVC.DTOs;
using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Services
{
    public class BookApplicationService : IBookApplicationService
    {
        private readonly IBooksService _books;
        private readonly IBookAuthorService _authors;
        private readonly IBookImageService _images;

        public BookApplicationService(
            IBooksService books,
            IBookAuthorService authors,
            IBookImageService images)
        {
            _books = books;
            _authors = authors;
            _images = images;
        }

        public async Task<Book> CreateAsync(
            BookInput input,
            IEnumerable<Guid>? authorIds,
            ICollection<IFormFile>? images)
        {
            var book = new Book(input.Isbn, input.Title, input.Summary, input.IsActive);
            book.MarkCreated();
            await _books.AddAsync(book);
            await _authors.UpdateAsync(book.Id, authorIds ?? Enumerable.Empty<Guid>());

            if (images != null && images.Count > 0)
                await _images.AddToBookAsync(book.Id, images);

            return book;
        }

        public async Task<Book?> UpdateAsync(
            Guid id,
            BookInput input,
            IEnumerable<Guid> authorIds,
            ICollection<IFormFile>? images)
        {
            var existingBook = await _books.GetByIdAsync(id);
            if (existingBook == null) return null;

            existingBook.UpdateDetails(input.Isbn, input.Title, input.Summary);

            if (input.IsActive)
                existingBook.Activate();
            else
                existingBook.Deactivate();

            await _books.UpdateAsync(existingBook);
            await _authors.UpdateAsync(id, authorIds);

            if (images != null && images.Count > 0)
                await _images.AddToBookAsync(id, images);

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
    }
}
