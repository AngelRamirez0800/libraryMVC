using System;
using System.Collections.Generic;
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

        public async Task<Author?> GetByIdWithBooksAsync(Guid id)
        {
            return await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

    }
}