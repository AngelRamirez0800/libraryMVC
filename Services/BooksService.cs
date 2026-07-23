using System;
using libraryMVC.Data.Repositories;
using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class BooksService : Service<Book, Guid>
    {
        public BooksService(BooksRepository repository) : base(repository)
        {
        }
    }
}