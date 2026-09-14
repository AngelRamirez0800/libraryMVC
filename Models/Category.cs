using System.ComponentModel.DataAnnotations;

namespace libraryMVC.Models
{
    public class Category : Entity<Guid>
    {
        public Category()
        {
        }

        public Category(string name, string description, bool isActive = true)
        {
            UpdateDetails(name, description);
            if (!isActive)
            {
                Deactivate();
            }
        }

        [Required]
        [StringLength(100)]
        public string Name { get; private set; } = string.Empty;

        [StringLength(250)]
        public string Description { get; private set; } = string.Empty;

        private readonly List<Book> _books = new();

        public IReadOnlyCollection<Book> Books => _books;

        public void UpdateDetails(string name, string description)
        {
            Name = name;
            Description = description;
            UpdatedAt = DateTime.Now;
        }

        public void ReplaceBooks(IEnumerable<Book> books)
        {
            _books.Clear();
            _books.AddRange(books);
        }
    }
}
