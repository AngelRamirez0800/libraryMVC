using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Interfaces;
using libraryMVC.Models;


namespace libraryMVC.Services
{
    public class AuthorsService : Service<Author, Guid>, IAuthorsService
    {
        private readonly IAuthorsRepository _repository;
        private readonly IAuthorBookService _authorBookService;

        public AuthorsService(
            IAuthorsRepository repository,
            IAuthorBookService authorBookService) : base(repository)
        {
            _repository = repository;
            _authorBookService = authorBookService;
        }

        public async Task<Author> CreateAsync(Author author, IEnumerable<Guid>? bookIds)
        {
            author.CreatedAt = DateTime.Now;
            await AddAsync(author);

            if (bookIds != null)
                await _authorBookService.UpdateAsync(author.Id, bookIds);

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

            await UpdateAsync(existingAuthor);
            await _authorBookService.UpdateAsync(id, bookIds);
            return existingAuthor;
        }

        public async Task<Author?> GetByIdWithBooksAsync(Guid id)
        {
            return await _repository.GetByIdWithBooksAsync(id);
        }

    }
}