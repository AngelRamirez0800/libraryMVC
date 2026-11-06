using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Services;

public sealed class EntityServicesTests
{
    [Fact]
    public async Task GenericServiceForwardsAllRepositoryContracts()
    {
        var reader = new Mock<IEntityReaderRepository<Book, Guid>>();
        var writer = new Mock<IEntityWriterRepository<Book, Guid>>();
        var lifecycle = new Mock<IEntityLifecycleRepository<Book, Guid>>();
        var service = new Service<Book, Guid>(reader.Object, writer.Object, lifecycle.Object);
        Book book = new("isbn", "Title", "Summary");
        Guid id = Guid.NewGuid();
        IList<Book?> expected = [book];

        writer.Setup(repository => repository.AddAsync(book)).ReturnsAsync(book);
        writer.Setup(repository => repository.UpdateAsync(book)).ReturnsAsync(book);
        reader.Setup(repository => repository.GetByIdAsync(id)).ReturnsAsync(book);
        reader.Setup(repository => repository.GetAllAsync()).ReturnsAsync(expected);
        reader.Setup(repository => repository.GetAllActiveAsync()).ReturnsAsync(expected);
        reader.Setup(repository => repository.GetAllInactiveAsync()).ReturnsAsync(expected);
        lifecycle.Setup(repository => repository.AlreadyExistsAsync(id)).ReturnsAsync(true);
        lifecycle.Setup(repository => repository.DeleteAsync(id)).ReturnsAsync(true);
        lifecycle.Setup(repository => repository.RestoreAsync(id)).ReturnsAsync(true);

        Assert.Same(book, await service.AddAsync(book));
        Assert.Same(book, await service.UpdateAsync(book));
        Assert.True(await service.DeleteAsync(id));
        Assert.True(await service.RestoreAsync(id));
        Assert.True(await service.AlreadyExistsAsync(id));
        Assert.Same(book, await service.GetByIdAsync(id));
        Assert.Same(expected, await service.GetAllAsync());
        Assert.Same(expected, await service.GetAllActiveAsync());
        Assert.Same(expected, await service.GetAllInactiveAsync());

        writer.Verify(repository => repository.AddAsync(book), Times.Once);
        writer.Verify(repository => repository.UpdateAsync(book), Times.Once);
        lifecycle.Verify(repository => repository.DeleteAsync(id), Times.Once);
        lifecycle.Verify(repository => repository.RestoreAsync(id), Times.Once);
        lifecycle.Verify(repository => repository.AlreadyExistsAsync(id), Times.Once);
        reader.Verify(repository => repository.GetByIdAsync(id), Times.Once);
        reader.Verify(repository => repository.GetAllAsync(), Times.Once);
        reader.Verify(repository => repository.GetAllActiveAsync(), Times.Once);
        reader.Verify(repository => repository.GetAllInactiveAsync(), Times.Once);
    }

    [Fact]
    public async Task BooksServiceForwardsDetailAndFeaturedQueries()
    {
        var repository = new Mock<IBooksRepository>();
        var service = new BooksService(repository.Object);
        Guid bookId = Guid.NewGuid();
        Book book = new("isbn", "Title", "Summary");
        List<Book> featured = [book];
        repository.Setup(value => value.GetByIdWithDetailsAsync(bookId)).ReturnsAsync(book);
        repository.Setup(value => value.GetRandomFeaturedBooksAsync(1)).ReturnsAsync(featured);

        Assert.Same(book, await service.GetByIdWithDetailsAsync(bookId));
        Assert.Same(featured, await service.GetFeaturedBooksAsync(1));

        repository.Verify(value => value.GetByIdWithDetailsAsync(bookId), Times.Once);
        repository.Verify(value => value.GetRandomFeaturedBooksAsync(1), Times.Once);
    }

    [Fact]
    public async Task AuthorsServiceCreatesAndUpdatesAuthorRelationships()
    {
        var repository = new Mock<IAuthorsRepository>();
        var authorBooks = new Mock<IAuthorBookService>();
        Guid createdId = Guid.NewGuid();
        repository.Setup(value => value.AddAsync(It.IsAny<Author>()))
            .Callback<Author>(author => SetId(author, createdId))
            .Returns((Author author) => Task.FromResult(author));
        var service = new AuthorsService(repository.Object, authorBooks.Object);
        var input = new AuthorInput
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Nationality = "British",
            BirthDate = new DateTime(1815, 12, 10),
            IsActive = false
        };
        Guid[] selectedBookIds = [Guid.NewGuid()];

        Author created = await service.CreateAsync(input, selectedBookIds);

        Assert.Equal(createdId, created.Id);
        Assert.False(created.IsActive);
        repository.Verify(value => value.AddAsync(It.Is<Author>(author =>
            author.FirstName == input.FirstName
            && author.LastName == input.LastName
            && author.Nationality == input.Nationality)), Times.Once);
        authorBooks.Verify(value => value.UpdateAsync(createdId, It.Is<IEnumerable<Guid>>(
            ids => ids.SequenceEqual(selectedBookIds))), Times.Once);

        Guid existingId = Guid.NewGuid();
        Author existing = new("Old", "Name", "Other", DateTime.UnixEpoch);
        SetId(existing, existingId);
        repository.Setup(value => value.GetByIdWithBooksAsync(existingId)).ReturnsAsync(existing);
        var updateInput = new AuthorInput
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Nationality = "American",
            BirthDate = new DateTime(1906, 12, 9)
        };
        Guid[] updatedBookIds = [Guid.NewGuid(), Guid.NewGuid()];

        Author? updated = await service.UpdateAsync(existingId, updateInput, updatedBookIds);

        Assert.Same(existing, updated);
        Assert.Equal("Grace", existing.FirstName);
        Assert.True(existing.IsActive);
        repository.Verify(value => value.UpdateAsync(existing), Times.Once);
        authorBooks.Verify(value => value.UpdateAsync(existingId, It.Is<IEnumerable<Guid>>(
            ids => ids.SequenceEqual(updatedBookIds))), Times.Once);
    }

    [Fact]
    public async Task AuthorServiceDoesNotUpdateRelationshipsForUnknownAuthor()
    {
        var repository = new Mock<IAuthorsRepository>();
        var authorBooks = new Mock<IAuthorBookService>();
        var service = new AuthorsService(repository.Object, authorBooks.Object);
        Guid authorId = Guid.NewGuid();

        Assert.Null(await service.UpdateAsync(authorId, new AuthorInput(), Array.Empty<Guid>()));

        repository.Verify(value => value.GetByIdWithBooksAsync(authorId), Times.Once);
        repository.Verify(value => value.UpdateAsync(It.IsAny<Author>()), Times.Never);
        authorBooks.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(),
            It.IsAny<IEnumerable<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task RelationshipServicesForwardUpdatesToTheirRepositories()
    {
        var authorBookRepository = new Mock<IAuthorBookRepository>();
        var bookAuthorRepository = new Mock<IBookAuthorRepository>();
        var categoryBookRepository = new Mock<ICategoryBookRepository>();
        var authorBookService = new AuthorBookService(authorBookRepository.Object);
        var bookAuthorService = new BookAuthorService(bookAuthorRepository.Object);
        var categoryBookService = new CategoryBookService(categoryBookRepository.Object);
        Guid entityId = Guid.NewGuid();
        Guid[] relatedIds = [Guid.NewGuid(), Guid.NewGuid()];

        await authorBookService.UpdateAsync(entityId, relatedIds);
        await bookAuthorService.UpdateAsync(entityId, relatedIds);
        await categoryBookService.UpdateAsync(entityId, relatedIds);
        await categoryBookService.UpdateForBookAsync(entityId, relatedIds);

        authorBookRepository.Verify(value => value.UpdateAsync(entityId, relatedIds), Times.Once);
        bookAuthorRepository.Verify(value => value.UpdateAsync(entityId, relatedIds), Times.Once);
        categoryBookRepository.Verify(value => value.UpdateAsync(entityId, relatedIds), Times.Once);
        categoryBookRepository.Verify(value => value.UpdateForBookAsync(entityId, relatedIds), Times.Once);
    }

    private static void SetId(Entity<Guid> entity, Guid id) =>
        typeof(Entity<Guid>).GetProperty(nameof(Entity<Guid>.Id))!.SetValue(entity, id);
}
