namespace libraryMVC.Models
{
    public class Book: Entity<Guid>
    {
        public required string Isbn { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        private readonly List<Author> _authors = new();
        private readonly List<BookImage> _images = new();

        public IReadOnlyCollection<Author> Authors => _authors;
        public IReadOnlyCollection<BookImage> Images => _images;

        public void ReplaceAuthors(IEnumerable<Author> authors)
        {
            _authors.Clear();
            _authors.AddRange(authors);
        }

        public void AddImage(BookImage image)
        {
            _images.Add(image);
        }

        public void RemoveImage(BookImage image)
        {
            _images.Remove(image);
        }
    }
}
