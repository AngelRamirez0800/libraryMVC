using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.Abstractions;
using libraryMVC.Interfaces;
using libraryMVC.Models;

namespace libraryMVC.Services
{
    public class Service<T, I> :
        IEntityReaderService<T, I>,
        IEntityWriterService<T, I>,
        IEntityLifecycleService<T, I>
        where T : Entity<I>
    {
        private readonly IEntityReaderRepository<T, I> _reader;
        private readonly IEntityWriterRepository<T, I> _writer;
        private readonly IEntityLifecycleRepository<T, I> _lifecycle;

        public Service(
            IEntityReaderRepository<T, I> reader,
            IEntityWriterRepository<T, I> writer,
            IEntityLifecycleRepository<T, I> lifecycle)
        {
            _reader = reader;
            _writer = writer;
            _lifecycle = lifecycle;
        }

        public async Task<T> AddAsync(T entity) => await _writer.AddAsync(entity);
        public async Task<T> UpdateAsync(T entity) => await _writer.UpdateAsync(entity);
        public async Task<bool> DeleteAsync(I id) => await _lifecycle.DeleteAsync(id);
        public async Task<bool> RestoreAsync(I id) => await _lifecycle.RestoreAsync(id);
        public async Task<bool> AlreadyExistsAsync(I id) => await _lifecycle.AlreadyExistsAsync(id);
        public async Task<T?> GetByIdAsync(I id) => await _reader.GetByIdAsync(id);
        public async Task<IList<T?>> GetAllAsync() => await _reader.GetAllAsync();
        public async Task<IList<T?>> GetAllActiveAsync() => await _reader.GetAllActiveAsync();
        public async Task<IList<T?>> GetAllInactiveAsync() => await _reader.GetAllInactiveAsync();
    }
}
