using libraryMVC.Interfaces;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Services
{
    public class ImageFileValidator : IImageFileValidator
    {
        private static readonly string[] AllowedExtensions = { ".webp", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public void Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo de imagen es requerido.");

            if (file.Length > MaxFileSize)
                throw new ArgumentException($"El archivo no debe exceder {MaxFileSize / (1024 * 1024)}MB.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("Solo se permiten archivos: .webp, .jpg, .jpeg, .png");
        }
    }
}
