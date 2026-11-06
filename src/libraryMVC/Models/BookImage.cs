using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace libraryMVC.Models
{
    public class BookImage : Entity<Guid>
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

        [Required]
        public Guid BookId
        {
            get; private set;
        }
        [ForeignKey(nameof(BookId))]
        public Book Book { get; private set; } = null!;

        [Required]
        [StringLength(500)]
        public string ImagePath { get; private set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ImageNumber
        {
            get; private set;
        }
        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; private set; } = string.Empty;

        [Range(1, long.MaxValue)]
        public long FileSize
        {
            get; private set;
        }

        [NotMapped]
        public bool IsPrimary => ImageNumber == 1;

        [NotMapped]
        public DateTime UploadedAt => CreatedAt;

        [NotMapped]
        public string FormattedFileSize => $"{FileSize / 1024.0:F2} KB";
    }
}
