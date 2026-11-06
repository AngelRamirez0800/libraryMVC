using System.ComponentModel.DataAnnotations;

namespace libraryMVC.DTOs
{
    public class BookInput
    {
        public Guid Id
        {
            get; set;
        }

        [Required]
        [StringLength(17)]
        public string Isbn { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Summary { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
