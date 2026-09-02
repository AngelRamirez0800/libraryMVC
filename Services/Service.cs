using libraryMVC.Abstractions;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace libraryMVC.Services
{
    public class Service<T, I> : IService<T, I> where T : Entity<I>
    {
        private readonly IRepository<T, I> _repository;

        public Service(IRepository<T, I> repository)
        {
            _repository = repository;
        }

        public async Task<T> AddAsync(T entity) => await _repository.AddAsync(entity);
        public async Task<T> UpdateAsync(T entity) => await _repository.UpdateAsync(entity);
        public async Task<bool> DeleteAsync(I id) => await _repository.DeleteAsync(id);
        public async Task<bool> RestoreAsync(I id) => await _repository.RestoreAsync(id);
        public async Task<bool> AlreadyExistsAsync(I id) => await _repository.AlreadyExistsAsync(id);
        public async Task<T?> GetByIdAsync(I id) => await _repository.GetByIdAsync(id);
        public async Task<IList<T?>> GetAllAsync() => await _repository.GetAllAsync();
        public async Task<IList<T?>> GetAllActiveAsync() => await _repository.GetAllActiveAsync();
        public async Task<IList<T?>> GetAllInactiveAsync() => await _repository.GetAllInactiveAsync();
    }
}