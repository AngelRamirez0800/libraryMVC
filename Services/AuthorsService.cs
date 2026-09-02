using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Data.Repositories;
using libraryMVC.Interfaces;
using libraryMVC.Models;


namespace libraryMVC.Services
{
    public class AuthorsService : Service<Author, Guid>, IAuthorsService
    {
        private readonly IAuthorsRepository _repository;

        public AuthorsService(IAuthorsRepository repository) : base(repository)
        {
            _repository = repository;
        }

        public async Task<Author> CreateAsync(Author author, IEnumerable<Guid>? bookIds)
        {
            author.CreatedAt = DateTime.Now;
            await AddAsync(author);

            if (bookIds != null)
                await UpdateAuthorBooksAsync(author.Id, bookIds);

            return author;
        }

        public async Task<Author?> UpdateAsync(Guid id, Author author, IEnumerable<Guid> bookIds)
        {
            var existingAuthor = await GetByIdWithBooksAsync(id);
            if (existingAuthor == null) return null;

            existingAuthor.FirstName = author.FirstName;
            existingAuthor.LastName = author.LastName;
            existingAuthor.Nationality = author.Nationality;
            existingAuthor.BirthDate = author.BirthDate;

            if (author.IsActive)
                existingAuthor.Activate();
            else
                existingAuthor.Deactivate();

            await UpdateSync(existingAuthor);
            await UpdateAuthorBooksAsync(id, bookIds);
            return existingAuthor;
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