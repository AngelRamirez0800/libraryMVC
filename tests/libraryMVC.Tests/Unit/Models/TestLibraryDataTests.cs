using libraryMVC.Tests.Support.Builders;
using Xunit;

namespace libraryMVC.Tests.Unit.Models;

public sealed class TestLibraryDataTests
{
    [Fact]
    public void CreateBuildsTheExpectedDeterministicGraphAndIndirectAuthorCategoryRelationships()
    {
        TestLibraryData first = TestLibraryData.Create();
        TestLibraryData second = TestLibraryData.Create();

        Assert.Equal(2, first.Authors.Count);
        Assert.Equal(10, first.Books.Count);
        Assert.Equal(5, first.Categories.Count);
        Assert.Equal(
            first.Books.Select(book => (book.Isbn, book.Title, book.Summary)),
            second.Books.Select(book => (book.Isbn, book.Title, book.Summary)));
        Assert.Equal(
            first.Authors.Select(author => (author.FirstName, author.LastName)),
            second.Authors.Select(author => (author.FirstName, author.LastName)));

        Assert.All(first.Books, book =>
        {
            Assert.NotEmpty(book.Authors);
            Assert.NotEmpty(book.Categories);
        });
        Assert.All(first.Authors, author => Assert.NotEmpty(author.Books));
        Assert.All(first.Categories, category => Assert.NotEmpty(category.Books));

        Assert.All(first.Authors, author =>
        {
            Assert.All(first.Categories, category =>
                Assert.Contains(author.Books, book => book.Categories.Contains(category)));
        });
    }
}
