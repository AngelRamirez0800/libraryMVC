using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class AuthorsRepository : Repository<Author, Guid>, IAuthorsRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthorsRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<Author?> GetByIdAsync(Guid id)
        {
            return await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Author?> GetByIdWithBooksAsync(Guid id)
        {
            return await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task UpdateAuthorBooksAsync(Guid authorId, IEnumerable<Guid> bookIds)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == authorId);

            if (author == null) return;

            author.Books.Clear();

            if (bookIds != null && bookIds.Any())
            {
                var selectedBooks = await _context.Books
                    .Where(b => bookIds.Contains(b.Id))
                    .ToListAsync();

                foreach (var book in selectedBooks)
                {
                    author.Books.Add(book);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}