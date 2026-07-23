namespace libraryMVC.Models
{
    public class Author: Entity<Guid>
    {
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String Nationality { get; set; } = String.Empty;
        public DateTime BirthDate { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
