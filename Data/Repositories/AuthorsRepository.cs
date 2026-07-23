using System;
using System.Threading.Tasks;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Repositories
{
    public class AuthorsRepository : Repository<Author, Guid>
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
    }
}