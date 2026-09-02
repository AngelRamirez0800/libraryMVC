using System.ComponentModel.DataAnnotations;

namespace libraryMVC.DTOs
{
    public class AuthorInput
    {
        public Guid Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string Nationality { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
