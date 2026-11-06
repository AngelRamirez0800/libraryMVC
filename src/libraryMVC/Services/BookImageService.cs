using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class BookImageService : IBookImageService
    {
        private readonly IBookImageStorage _storage;
        private readonly IBookImageRepository _repository;
        private readonly IImageFileValidator _validator;

        public BookImageService(
            IBookImageStorage storage,
            IBookImageRepository repository,
            IImageFileValidator validator)
        {
            _storage = storage;
            _repository = repository;
            _validator = validator;
        }

        public async Task AddToBookAsync(Guid bookId, ICollection<IFormFile> imageFiles)
        {
            if (imageFiles == null || imageFiles.Count == 0)
            {
                throw new ArgumentException("Debes seleccionar al menos una imagen.");
            }

            foreach (var imageFile in imageFiles)
            {
                _validator.Validate(imageFile);
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
            {
                throw new InvalidOperationException("Imagen no encontrada.");
            }

            await _repository.RemoveAsync(bookImage);
            _storage.Delete(bookImage);
        }

    }
}
