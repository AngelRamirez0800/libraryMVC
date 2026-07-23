namespace libraryMVC.Models
{
    public class Book: Entity<Guid>
    {
        public required string Isbn { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public ICollection<Author> Authors { get; set; } = new List<Author>();
    }
}
