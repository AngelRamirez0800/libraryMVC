namespace libraryMVC.Models
{
    public class BookImage: Entity<Guid>
    {

        public Guid BookId { get; set; }
        public Book Book { get; set; }
        public required string ImagePath { get; set; }
        public required int ImageNumber { get; set; }
        public required string OriginalFileName { get; set; }
        public long FileSize { get; set; }

        public bool IsPrimary => ImageNumber == 1;
        public DateTime UploadedAt => CreatedAt;
        public string FormattedFileSize => $"{FileSize / 1024.0:F2} KB";
    }
}
