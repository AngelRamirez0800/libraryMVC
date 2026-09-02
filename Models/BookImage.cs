namespace libraryMVC.Models
{
    public class BookImage: Entity<Guid>
    {
        private BookImage()
        {
        }

        public BookImage(Guid bookId, string imagePath, int imageNumber, string originalFileName, long fileSize)
        {
            BookId = bookId;
            ImagePath = imagePath;
            ImageNumber = imageNumber;
            OriginalFileName = originalFileName;
            FileSize = fileSize;
            MarkCreated();
        }

        public Guid BookId { get; private set; }
        public Book Book { get; private set; } = null!;
        public string ImagePath { get; private set; } = string.Empty;
        public int ImageNumber { get; private set; }
        public string OriginalFileName { get; private set; } = string.Empty;
        public long FileSize { get; private set; }

        public bool IsPrimary => ImageNumber == 1;
        public DateTime UploadedAt => CreatedAt;
        public string FormattedFileSize => $"{FileSize / 1024.0:F2} KB";
    }
}
