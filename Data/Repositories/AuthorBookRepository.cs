using libraryMVC.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class AuthorBookRepository : IAuthorBookRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthorBookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateAsync(Guid authorId, IEnumerable<Guid> bookIds)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == authorId);

            if (author == null) return;

            author.Books.Clear();
            var selectedBooks = await _context.Books
                .Where(book => bookIds.Contains(book.Id))
                .ToListAsync();

            foreach (var book in selectedBooks)
                author.Books.Add(book);

            await _context.SaveChangesAsync();
        }
    }
}
