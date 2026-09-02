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

        public override async Task<Book?> GetByIdAsync(Guid id)
        {
            return await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
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

        public async Task UpdateBookAuthorsAsync(Guid bookId, IEnumerable<Guid> authorIds)
        {
            var book = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null) return;

            book.Authors.Clear();

            if (authorIds != null && authorIds.Any())
            {
                var selectedAuthors = await _context.Authors
                    .Where(a => authorIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var author in selectedAuthors)
                {
                    book.Authors.Add(author);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<BookImage?> GetBookImageByIdAsync(Guid imageId)
        {
            return await _context.BookImages.FirstOrDefaultAsync(bi => bi.Id == imageId);
        }

        public async Task RemoveBookImageAsync(BookImage bookImage)
        {
            _context.BookImages.Remove(bookImage);
            await Task.CompletedTask;
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

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}