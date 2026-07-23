using System;
using libraryMVC.Data.Repositories;
using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class AuthorsService : Service<Author, Guid>
    {
        public AuthorsService(AuthorsRepository repository) : base(repository)
        {
        }
    }
}