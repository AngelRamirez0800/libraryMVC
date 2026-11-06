using libraryMVC.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class CategoryBookRepository : ICategoryBookRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryBookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateAsync(Guid categoryId, IEnumerable<Guid> bookIds)
        {
            var category = await _context.Categories
                .Include(item => item.Books)
                .FirstOrDefaultAsync(item => item.Id == categoryId);

            if (category == null)
            {
                return;
            }

            var selectedBooks = await _context.Books
                .Where(book => bookIds.Distinct().Contains(book.Id))
                .ToListAsync();

            var selectedBookIdSet = selectedBooks.Select(book => book.Id).ToHashSet();
            var removedBookIds = category.Books
                .Select(book => book.Id)
                .Where(bookId => !selectedBookIdSet.Contains(bookId))
                .ToList();

            if (removedBookIds.Count > 0)
            {
                var booksWithOtherCategories = await _context.Books
                    .Where(book => removedBookIds.Contains(book.Id) && book.Categories.Count() > 1)
                    .Select(book => book.Id)
                    .ToListAsync();

                var invalidBookIds = removedBookIds.Except(booksWithOtherCategories).ToList();
                if (invalidBookIds.Count > 0)
                {
                    throw new InvalidOperationException("No puedes quitar la última categoría de un libro.");
                }
            }

            category.ReplaceBooks(selectedBooks);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateForBookAsync(Guid bookId, IEnumerable<Guid> categoryIds)
        {
            var book = await _context.Books
                .Include(item => item.Categories)
                .FirstOrDefaultAsync(item => item.Id == bookId);

            if (book == null)
            {
                return;
            }

            var selectedCategories = await _context.Categories
                .Where(category => categoryIds.Distinct().Contains(category.Id))
                .ToListAsync();

            book.ReplaceCategories(selectedCategories);
            await _context.SaveChangesAsync();
        }
    }
}
