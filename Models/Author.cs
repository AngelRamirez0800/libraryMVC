namespace libraryMVC.Models
{
    public class Author: Entity<Guid>
    {
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String Nationality { get; set; } = String.Empty;
        public DateTime BirthDate { get; set; }

        private readonly List<Book> _books = new();

        public IReadOnlyCollection<Book> Books => _books;

        public void ReplaceBooks(IEnumerable<Book> books)
        {
            _books.Clear();
            _books.AddRange(books);
        }
    }
}
