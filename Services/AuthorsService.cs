using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Data.Repositories;
using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class AuthorsService : Service<Author, Guid>
    {
        private readonly AuthorsRepository _repository;

        public AuthorsService(AuthorsRepository repository) : base(repository)
        {
            _repository = repository;
        }

        public async Task<Author?> GetByIdWithBooksAsync(Guid id)
        {
            return await _repository.GetByIdWithBooksAsync(id);
        }

        public async Task UpdateAuthorBooksAsync(Guid authorId, IEnumerable<Guid> bookIds)
        {
            await _repository.UpdateAuthorBooksAsync(authorId, bookIds);
        }
    }
}