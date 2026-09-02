using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class BooksRepository : Repository<Book, Guid>, IBooksRepository
    {
        private readonly ApplicationDbContext _context;

        public BooksRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Book?> GetByIdWithImagesAsync(Guid id)
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Images.OrderBy(i => i.ImageNumber))
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Book?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Images.OrderBy(i => i.ImageNumber))
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<Book>> GetRandomFeaturedBooksAsync(int count = 3)
        {
            var activeBooks = await _context.Books
                .Where(b => b.IsActive)
                .Include(b => b.Authors)
                .Include(b => b.Images.OrderBy(i => i.ImageNumber))
                .ToListAsync();

            return activeBooks
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .ToList();
        }

    }
}