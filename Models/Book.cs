namespace libraryMVC.Models
{
    public class Book : Entity<Guid>
    {
        public Book()
        {
        }

        public Book(string isbn, string title, string summary, bool isActive = true)
        {
            Isbn = isbn;
            UpdateDetails(isbn, title, summary);
            if (!isActive) Deactivate();
        }

        public string Isbn { get; private set; } = string.Empty;
        public string Title { get; private set; } = string.Empty;
        public string Summary { get; private set; } = string.Empty;
        private readonly List<Author> _authors = new();
        private readonly List<BookImage> _images = new();

        public IReadOnlyCollection<Author> Authors => _authors;
        public IReadOnlyCollection<BookImage> Images => _images;

        public void UpdateDetails(string isbn, string title, string summary)
        {
            Isbn = isbn;
            Title = title;
            Summary = summary;
            UpdatedAt = DateTime.Now;
        }

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
