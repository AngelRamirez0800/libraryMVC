using Microsoft.AspNetCore.Http;

namespace libraryMVC.Interfaces
{
    public interface IImageFileValidator
    {
        void Validate(IFormFile file);
    }
}
