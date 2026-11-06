using System.ComponentModel.DataAnnotations;

namespace libraryMVC.Models
{
    public class Author : Entity<Guid>
    {
        public Author()
        {
        }

        public Author(string firstName, string lastName, string nationality, DateTime birthDate, bool isActive = true)
        {
            UpdateDetails(firstName, lastName, nationality, birthDate);
            if (!isActive)
            {
                Deactivate();
            }
        }

        [Required]
        [StringLength(100)]
        public String FirstName { get; private set; } = String.Empty;

        [Required]
        [StringLength(100)]
        public String LastName { get; private set; } = String.Empty;

        [Required]
        [StringLength(100)]
        public String Nationality { get; private set; } = String.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime BirthDate
        {
            get; private set;
        }

        private readonly List<Book> _books = new();

        public IReadOnlyCollection<Book> Books => _books;

        public void UpdateDetails(string firstName, string lastName, string nationality, DateTime birthDate)
        {
            FirstName = firstName;
            LastName = lastName;
            Nationality = nationality;
            BirthDate = birthDate;
            UpdatedAt = DateTime.Now;
        }

        public void ReplaceBooks(IEnumerable<Book> books)
        {
            _books.Clear();
            _books.AddRange(books);
        }
    }
}
