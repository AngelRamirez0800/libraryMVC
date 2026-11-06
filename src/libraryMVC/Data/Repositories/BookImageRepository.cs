using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class BookImageRepository : IBookImageRepository
    {
        private readonly ApplicationDbContext _context;

        public BookImageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookImage> AddAsync(BookImage image)
        {
            await _context.BookImages.AddAsync(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<BookImage?> GetByIdAsync(Guid imageId)
        {
            return await _context.BookImages.FirstOrDefaultAsync(image => image.Id == imageId);
        }

        public async Task RemoveAsync(BookImage image)
        {
            _context.BookImages.Remove(image);
            await _context.SaveChangesAsync();
        }
    }
}
