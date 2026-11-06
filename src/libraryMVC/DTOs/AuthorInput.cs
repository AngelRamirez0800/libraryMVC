using System.ComponentModel.DataAnnotations;

namespace libraryMVC.DTOs
{
    public class AuthorInput
    {
        public Guid Id
        {
            get; set;
        }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nationality { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime BirthDate
        {
            get; set;
        }
        public bool IsActive { get; set; } = true;
    }
}
