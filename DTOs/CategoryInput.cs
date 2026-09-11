using System.ComponentModel.DataAnnotations;

namespace libraryMVC.DTOs
{
    public class CategoryInput
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
