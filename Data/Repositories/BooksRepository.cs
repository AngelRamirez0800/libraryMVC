using System;
using System.Threading.Tasks;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class BooksRepository : Repository<Book, Guid>
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
    }
}