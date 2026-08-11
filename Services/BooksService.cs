using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Data.Repositories;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Services
{
    public class BooksService : Service<Book, Guid>
    {
        private readonly BooksRepository _repository;
        private readonly BookImageService _imageService;

        public BooksService(BooksRepository repository, BookImageService imageService) : base(repository)
        {
            _repository = repository;
            _imageService = imageService;
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