using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class BookImageService
    {
        
        private readonly IWebHostEnvironment _environment;
        private readonly string _imagesDirectory = "images/books";
        private readonly string[] _allowedExtensions = { ".webp", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public BookImageService (IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<BookImage> SaveBookImageAsync(Guid bookId, IFormFile imageFile)
        {
            ValidateImageFile(imageFile);

            string bookImagesDirectory = Path.Combine(
                _environment.WebRootPath,
                _imagesDirectory,
                bookId.ToString()
            );

            if (!Directory.Exists(bookImagesDirectory))
                Directory.CreateDirectory(bookImagesDirectory);

            int imageNumber = GetNextImageNumber(bookImagesDirectory);

            string extension = Path.GetExtension(imageFile.FileName).ToLower();
            if (string.IsNullOrEmpty(extension)) extension = ".webp";

            string fileName = $"{imageNumber:D2}{extension}";
            string filePath = Path.Combine(bookImagesDirectory, fileName);


            await SaveImageAsync(imageFile, filePath);

            var bookImage = new BookImage
            {
                BookId = bookId,
                ImagePath = $"{_imagesDirectory}/{bookId}/{fileName}",
                ImageNumber = imageNumber,
                OriginalFileName = imageFile.FileName,
                FileSize = new FileInfo(filePath).Length,
            };

            return bookImage;

        }

        public void DeleteBookImage(BookImage bookImage)
        {
            string filePath = Path.Combine(_environment.WebRootPath, bookImage.ImagePath.TrimStart('/'));

            if (File.Exists(filePath))
                File.Delete(filePath);

            string directoryPath = Path.GetDirectoryName(filePath);
            if (Directory.Exists(directoryPath) &&
                Directory.GetFiles(directoryPath).Length == 0)
            {
                Directory.Delete(directoryPath);
            }
        }

        private async Task SaveImageAsync(IFormFile imageFile, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
        }

        private void ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo de imagen es requerido.");

            if (file.Length > MaxFileSize)
                throw new ArgumentException($"El archivo no debe exceder {MaxFileSize / (1024 * 1024)}MB.");

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(extension))
                throw new ArgumentException("Solo se permiten archivos: .webp, .jpg, .jpeg, .png");
        }

        private int GetNextImageNumber(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 1;

            var files = Directory.GetFiles(directoryPath);
            return files.Length + 1;
        }
    }
}
