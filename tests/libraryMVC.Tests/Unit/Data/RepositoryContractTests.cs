using System.Collections;
using System.Linq.Expressions;
using libraryMVC.Data;
using libraryMVC.Data.Repositories;
using libraryMVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Data;

public class RepositoryContractTests
{
    [Fact]
    public async Task AddAsyncReturnsEntityAndSavesChanges()
    {
        var (repository, context, set, _) = CreateRepository();
        var book = new Book("978-1-234567-89-0", "Novel", "A story");

        var result = await repository.AddAsync(book);

        Assert.Same(book, result);
        set.Verify(value => value.AddAsync(book, It.IsAny<CancellationToken>()), Times.Once);
        context.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncDeactivatesAnActiveEntityAndSavesChanges()
    {
        var id = Guid.NewGuid();
        var book = CreateBook(id);
        var (repository, context, set, _) = CreateRepository(book);

        var deleted = await repository.DeleteAsync(id);

        Assert.True(deleted);
        Assert.False(book.IsActive);
        Assert.NotNull(book.DeletedAt);
        set.Verify(value => value.Update(book), Times.Once);
        context.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsyncReturnsFalseForUnknownEntityWithoutSaving()
    {
        var (repository, context, set, _) = CreateRepository();

        var restored = await repository.RestoreAsync(Guid.NewGuid());

        Assert.False(restored);
        set.Verify(value => value.Update(It.IsAny<Book>()), Times.Never);
        context.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenEntityIsMissing()
    {
        var (repository, _, _, _) = CreateRepository();

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ActiveAndInactiveQueriesReturnOnlyMatchingEntities()
    {
        var activeBook = CreateBook(Guid.NewGuid());
        var inactiveBook = new Book("978-1-234567-89-1", "Archived", "An old story", isActive: false);
        var (repository, _, _, _) = CreateRepository(activeBook, inactiveBook);

        var active = await repository.GetAllActiveAsync();
        var inactive = await repository.GetAllInactiveAsync();

        Assert.Equal(new[] { activeBook }, active);
        Assert.Equal(new[] { inactiveBook }, inactive);
    }

    private static (
        Repository<Book, Guid> Repository,
        Mock<ApplicationDbContext> Context,
        Mock<DbSet<Book>> Set,
        List<Book> Rows)
        CreateRepository(params Book[] books)
    {
        var rows = books.ToList();
        var set = CreateDbSet(rows);
        set.Setup(value => value.FindAsync(It.IsAny<object?[]>()))
            .Returns((object?[] keys) =>
        {
            return ValueTask.FromResult(rows.FirstOrDefault(book => Equals(book.Id, keys[0])));
        });
        var context = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        context.Setup(value => value.Set<Book>()).Returns(set.Object);
        context.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return (new Repository<Book, Guid>(context.Object), context, set, rows);
    }

    private static Mock<DbSet<T>> CreateDbSet<T>(IList<T> rows) where T : class
    {
        var set = new Mock<DbSet<T>>();
        var query = rows.AsQueryable();
        var asyncProvider = new AsyncQueryProvider(query.Provider);

        set.As<IQueryable<T>>().Setup(value => value.Provider).Returns(asyncProvider);
        set.As<IQueryable<T>>().Setup(value => value.Expression).Returns(query.Expression);
        set.As<IQueryable<T>>().Setup(value => value.ElementType).Returns(query.ElementType);
        set.As<IQueryable<T>>().Setup(value => value.GetEnumerator()).Returns(() => query.GetEnumerator());
        set.As<IAsyncEnumerable<T>>()
            .Setup(value => value.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) => new AsyncEnumerator<T>(query.GetEnumerator()));
        set.Setup(value => value.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Returns((T _, CancellationToken _) => ValueTask.FromResult<EntityEntry<T>>(null!));
        return set;
    }

    private static Book CreateBook(Guid id)
    {
        var book = new Book("978-1-234567-89-0", "Novel", "A story");
        typeof(Entity<Guid>).GetProperty(nameof(Entity<Guid>.Id))!.SetValue(book, id);
        return book;
    }

    private sealed class AsyncEnumerator<T>(IEnumerator<T> enumerator) : IAsyncEnumerator<T>
    {
        public T Current => enumerator.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(enumerator.MoveNext());
        public ValueTask DisposeAsync()
        {
            enumerator.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AsyncQueryProvider(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments().First();
            var query = inner.CreateQuery(expression);
            return (IQueryable)Activator.CreateInstance(
                typeof(AsyncQueryable<>).MakeGenericType(elementType), query, this)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new AsyncQueryable<TElement>(inner.CreateQuery<TElement>(expression), this);

        public object? Execute(Expression expression) => inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var result = typeof(IQueryProvider)
                .GetMethods()
                .Single(method => method.Name == nameof(Execute) && method.IsGenericMethod)
                .MakeGenericMethod(resultType)
                .Invoke(inner, new object[] { expression });
            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, new[] { result })!;
        }
    }

    private sealed class AsyncQueryable<T>(IQueryable<T> inner, IAsyncQueryProvider provider) :
        IQueryable<T>, IAsyncEnumerable<T>
    {
        public Type ElementType => inner.ElementType;
        public Expression Expression => inner.Expression;
        public IQueryProvider Provider => provider;
        public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new AsyncEnumerator<T>(inner.GetEnumerator());
    }
}
