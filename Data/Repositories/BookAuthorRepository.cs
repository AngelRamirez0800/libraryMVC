using libraryMVC.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class BookAuthorRepository : IBookAuthorRepository
    {
        private readonly ApplicationDbContext _context;

        public BookAuthorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateAsync(Guid bookId, IEnumerable<Guid> authorIds)
        {
            var book = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null) return;

            book.Authors.Clear();
            var selectedAuthors = await _context.Authors
                .Where(a => authorIds.Contains(a.Id))
                .ToListAsync();

            foreach (var author in selectedAuthors)
                book.Authors.Add(author);

            await _context.SaveChangesAsync();
        }
    }
}
