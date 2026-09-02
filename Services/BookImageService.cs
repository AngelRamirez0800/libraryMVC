using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class BookImageService : IBookImageService
    {
        
        private readonly IBookImageStorage _storage;
        private readonly string[] _allowedExtensions = { ".webp", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public BookImageService(IBookImageStorage storage)
        {
            _storage = storage;
        }

        public async Task<BookImage> SaveBookImageAsync(Guid bookId, IFormFile imageFile)
        {
            ValidateImageFile(imageFile);

            var storedImage = await _storage.SaveAsync(bookId, imageFile);

            var bookImage = new BookImage
            {
                BookId = bookId,
                ImagePath = storedImage.ImagePath,
                ImageNumber = storedImage.ImageNumber,
                OriginalFileName = imageFile.FileName,
                FileSize = storedImage.FileSize,
            };

            return bookImage;

        }

        public void DeleteBookImage(BookImage bookImage)
        {
            _storage.Delete(bookImage);
        }

        private void ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo de imagen es requerido.");

            if (file.Length > MaxFileSize)
                throw new ArgumentException($"El archivo no debe exceder {MaxFileSize / (1024 * 1024)}MB.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new ArgumentException("Solo se permiten archivos: .webp, .jpg, .jpeg, .png");
        }
    }
}
