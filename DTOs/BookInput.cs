using System.ComponentModel.DataAnnotations;

namespace libraryMVC.DTOs
{
    public class BookInput
    {
        public Guid Id { get; set; }

        [Required]
        public string Isbn { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
