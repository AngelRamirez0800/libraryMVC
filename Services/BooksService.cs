using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Data.Repositories;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Http;

namespace libraryMVC.Services
{
    public class BooksService : Service<Book, Guid>, IBooksService, IBookQueries, IBookLifecycle
    {
        private readonly IBooksRepository _repository;

        public BooksService(IBooksRepository repository) : base(repository, repository, repository)
        {
            _repository = repository;
        }

        public async Task<Book?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _repository.GetByIdWithDetailsAsync(id);
        }

        public async Task<List<Book>> GetFeaturedBooksAsync(int count = 3)
        {
            return await _repository.GetRandomFeaturedBooksAsync(count);
        }

    }
}