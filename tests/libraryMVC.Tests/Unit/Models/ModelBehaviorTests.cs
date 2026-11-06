using System.Globalization;
using libraryMVC.Models;
using Xunit;

namespace libraryMVC.Tests.Unit.Models;

public sealed class ModelBehaviorTests
{
    [Fact]
    public void EntityLifecycleUpdatesActivityAndTimestamps()
    {
        Entity<Guid> entity = new();

        entity.MarkCreated();
        DateTime createdAt = entity.CreatedAt;
        entity.Deactivate();

        Assert.NotEqual(default, createdAt);
        Assert.False(entity.IsActive);
        Assert.NotNull(entity.DeletedAt);
        Assert.NotNull(entity.UpdatedAt);

        entity.Activate();

        Assert.True(entity.IsActive);
        Assert.Null(entity.DeletedAt);
        Assert.NotNull(entity.UpdatedAt);
    }

    [Fact]
    public void UpdateDetailsAndRelationshipMethodsReplaceTheirValues()
    {
        Author author = new("Ada", "Lovelace", "British", new DateTime(1815, 12, 10));
        Book firstBook = new("one", "First", "Summary");
        Book secondBook = new("two", "Second", "Summary");
        Category category = new("History", "Historical works");

        author.ReplaceBooks(new[] { firstBook, secondBook });
        author.ReplaceBooks(new[] { secondBook });
        category.ReplaceBooks(new[] { firstBook, secondBook });
        category.ReplaceBooks(new[] { secondBook });
        firstBook.ReplaceAuthors(new[] { author });
        firstBook.ReplaceCategories(new[] { category });

        Assert.Single(author.Books);
        Assert.Same(secondBook, Assert.Single(author.Books));
        Assert.Single(category.Books);
        Assert.Same(secondBook, Assert.Single(category.Books));
        Assert.Same(author, Assert.Single(firstBook.Authors));
        Assert.Same(category, Assert.Single(firstBook.Categories));
        Assert.NotNull(author.UpdatedAt);
    }

    [Fact]
    public void BookImagesCanBeAddedAndRemovedAndExposeDerivedMetadata()
    {
        Book book = new("isbn", "Title", "Summary");
        BookImage primaryImage = new(Guid.NewGuid(), "cover.jpg", 1, "cover.jpg", 1_048_576);
        BookImage secondaryImage = new(Guid.NewGuid(), "inside.jpg", 2, "inside.jpg", 512);

        book.AddImage(primaryImage);
        book.AddImage(secondaryImage);

        Assert.Equal(2, book.Images.Count);
        Assert.True(primaryImage.IsPrimary);
        Assert.False(secondaryImage.IsPrimary);
        Assert.Equal(primaryImage.CreatedAt, primaryImage.UploadedAt);
        string expectedFileSize = (primaryImage.FileSize / 1024.0).ToString("F2", CultureInfo.CurrentCulture) + " KB";
        Assert.Equal(expectedFileSize, primaryImage.FormattedFileSize);

        book.RemoveImage(primaryImage);

        Assert.Same(secondaryImage, Assert.Single(book.Images));
    }

    [Fact]
    public void ErrorViewModelOnlyShowsNonemptyRequestIds()
    {
        ErrorViewModel model = new();

        Assert.False(model.ShowRequestId);

        model.RequestId = "request-123";

        Assert.True(model.ShowRequestId);

        model.RequestId = string.Empty;

        Assert.False(model.ShowRequestId);
    }
}
