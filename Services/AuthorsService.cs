using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Interfaces;
using libraryMVC.DTOs;
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

        public async Task<Author> CreateAsync(AuthorInput input, IEnumerable<Guid>? bookIds)
        {
            var author = new Author();
            author.UpdateDetails(input.FirstName, input.LastName, input.Nationality, input.BirthDate);
            if (!input.IsActive) author.Deactivate();
            author.MarkCreated();
            await AddAsync(author);

            if (bookIds != null)
                await _authorBookService.UpdateAsync(author.Id, bookIds);

            return author;
        }

        public async Task<Author?> UpdateAsync(Guid id, AuthorInput input, IEnumerable<Guid> bookIds)
        {
            var existingAuthor = await GetByIdWithBooksAsync(id);
            if (existingAuthor == null) return null;

            existingAuthor.UpdateDetails(input.FirstName, input.LastName, input.Nationality, input.BirthDate);

            if (input.IsActive)
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