using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Data.Storage
{
    public class LocalBookImageStorage : IBookImageStorage
    {
        private const string ImagesDirectory = "images/books";
        private readonly IWebHostEnvironment _environment;

        public LocalBookImageStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<StoredBookImage> SaveAsync(Guid bookId, IFormFile imageFile)
        {
            string bookImagesDirectory = Path.Combine(
                _environment.WebRootPath,
                ImagesDirectory,
                bookId.ToString());

            Directory.CreateDirectory(bookImagesDirectory);

            string extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".webp";
            }

            for (int imageNumber = 1; ; imageNumber = checked(imageNumber + 1))
            {
                string fileName = $"{imageNumber:D2}{extension}";
                string filePath = Path.Combine(bookImagesDirectory, fileName);
                FileStream stream;

                try
                {
                    stream = new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 4096,
                        FileOptions.Asynchronous);
                }
                catch (IOException exception) when (IsFileAlreadyExists(exception))
                {
                    continue;
                }

                try
                {
                    await using (stream)
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                }
                catch
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    throw;
                }

                return new StoredBookImage(
                    $"{ImagesDirectory}/{bookId}/{fileName}",
                    imageNumber,
                    new FileInfo(filePath).Length);
            }
        }

        public void Delete(BookImage bookImage)
        {
            string filePath = Path.Combine(
                _environment.WebRootPath,
                bookImage.ImagePath.TrimStart('/'));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            string? directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath != null &&
                Directory.Exists(directoryPath) &&
                Directory.GetFiles(directoryPath).Length == 0)
            {
                Directory.Delete(directoryPath);
            }
        }

        private static bool IsFileAlreadyExists(IOException exception)
        {
            int nativeErrorCode = exception.HResult & 0xFFFF;
            return nativeErrorCode is 17 or 80 or 183;
        }
    }
}
