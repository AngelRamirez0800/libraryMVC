using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class BookImageService : IBookImageService
    {
        private readonly IBookImageStorage _storage;
        private readonly IBookImageRepository _repository;
        private readonly string[] _allowedExtensions = { ".webp", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public BookImageService(IBookImageStorage storage, IBookImageRepository repository)
        {
            _storage = storage;
            _repository = repository;
        }

        public async Task AddToBookAsync(Guid bookId, ICollection<IFormFile> imageFiles)
        {
            if (imageFiles == null || imageFiles.Count == 0)
                throw new ArgumentException("Debes seleccionar al menos una imagen.");

            foreach (var imageFile in imageFiles)
            {
                ValidateImageFile(imageFile);
                var storedImage = await _storage.SaveAsync(bookId, imageFile);

                var bookImage = new BookImage(
                    bookId,
                    storedImage.ImagePath,
                    storedImage.ImageNumber,
                    imageFile.FileName,
                    storedImage.FileSize);

                await _repository.AddAsync(bookImage);
            }
        }

        public async Task RemoveAsync(Guid imageId)
        {
            var bookImage = await _repository.GetByIdAsync(imageId);
            if (bookImage == null)
                throw new InvalidOperationException("Imagen no encontrada.");

            await _repository.RemoveAsync(bookImage);
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
