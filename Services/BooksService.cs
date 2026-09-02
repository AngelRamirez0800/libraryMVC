using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Data.Repositories;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Services
{
    public class BooksService : Service<Book, Guid>, IBooksService
    {
        private readonly IBooksRepository _repository;
        private readonly IBookImageService _imageService;

        public BooksService(IBooksRepository repository, IBookImageService imageService) : base(repository)
        {
            _repository = repository;
            _imageService = imageService;
        }

        public async Task<Book> CreateAsync(Book book, IEnumerable<Guid>? authorIds, ICollection<IFormFile>? images)
        {
            book.CreatedAt = DateTime.Now;
            await AddAsync(book);

            if (authorIds != null)
                await UpdateBookAuthorsAsync(book.Id, authorIds);

            if (images != null && images.Count > 0)
                await AddImagesToBookAsync(book.Id, images);

            return book;
        }

        public async Task<Book?> UpdateAsync(Guid id, Book book, IEnumerable<Guid> authorIds, ICollection<IFormFile>? images)
        {
            var existingBook = await GetByIdWithDetailsAsync(id);
            if (existingBook == null) return null;

            existingBook.Isbn = book.Isbn;
            existingBook.Title = book.Title;
            existingBook.Summary = book.Summary;

            if (book.IsActive)
                existingBook.Activate();
            else
                existingBook.Deactivate();

            await UpdateSync(existingBook);
            await UpdateBookAuthorsAsync(id, authorIds);

            if (images != null && images.Count > 0)
                await AddImagesToBookAsync(id, images);

            return existingBook;
        }

        public async Task<Book?> GetByIdWithImagesAsync(Guid id)
        {
            return await _repository.GetByIdWithImagesAsync(id);
        }

        public async Task<Book?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _repository.GetByIdWithDetailsAsync(id);
        }

        public async Task<List<Book>> GetFeaturedBooksAsync(int count = 3)
        {
            return await _repository.GetRandomFeaturedBooksAsync(count);
        }

        public async Task UpdateBookAuthorsAsync(Guid bookId, IEnumerable<Guid> authorIds)
        {
            await _repository.UpdateBookAuthorsAsync(bookId, authorIds);
        }

        public async Task<Book> AddImagesToBookAsync(Guid bookId, ICollection<IFormFile> imageFiles)
        {
            if (imageFiles == null || imageFiles.Count == 0)
                throw new ArgumentException("Debes seleccionar al menos una imagen.");

            var book = await GetByIdWithImagesAsync(bookId);
            if (book == null)
                throw new InvalidOperationException($"Libro con ID {bookId} no encontrado.");

            foreach (var imageFile in imageFiles)
            {
                try
                {
                    var bookImage = await _imageService.SaveBookImageAsync(bookId, imageFile);
                    book.Images.Add(bookImage);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error al guardar imagen: {ex.Message}", ex);
                }
            }

            await _repository.SaveChangesAsync();
            return book;
        }

        public async Task RemoveImageFromBookAsync(Guid bookImageId)
        {
            var bookImage = await _repository.GetBookImageByIdAsync(bookImageId);
            if (bookImage == null)
                throw new InvalidOperationException("Imagen no encontrada.");

            try
            {
                _imageService.DeleteBookImage(bookImage);
                await _repository.RemoveBookImageAsync(bookImage);
                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al eliminar imagen: {ex.Message}", ex);
            }
        }
    }
}