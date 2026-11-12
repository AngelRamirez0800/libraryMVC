using libraryMVC.Data.Seeding;
using Xunit;

namespace libraryMVC.Tests.Unit.Data.Seeding;

public sealed class LiteratureSeedCatalogTests
{
    [Fact]
    public void CatalogContainsThreeAuthorsAndAtLeastSevenUniqueValidEditionsPerAuthor()
    {
        Assert.Equal(3, LiteratureSeedCatalog.Authors.Count);
        Assert.All(LiteratureSeedCatalog.Authors, author => Assert.True(author.Books.Count >= 7));
        Assert.Equal(
            LiteratureSeedCatalog.Authors.Count,
            LiteratureSeedCatalog.Authors
                .Select(author => (author.FirstName, author.LastName, author.BirthDate.Date))
                .Distinct()
                .Count());

        LiteratureBookSeed[] books = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .ToArray();
        Assert.Equal(21, books.Length);
        Assert.Equal(books.Length, books.Select(book => book.Isbn).Distinct(StringComparer.Ordinal).Count());
        Assert.All(books, book => Assert.True(IsValidIsbn13(book.Isbn), $"Invalid ISBN-13: {book.Isbn}"));
    }

    [Fact]
    public void EveryBookReferencesKnownEnglishCategoriesAndAvailableImageAssets()
    {
        HashSet<string> categoryNames = LiteratureSeedCatalog.Categories
            .Select(category => category.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(27, categoryNames.Count);
        Assert.Equal(LiteratureSeedCatalog.Categories.Count, categoryNames.Count);

        foreach (LiteratureAuthorSeed author in LiteratureSeedCatalog.Authors)
        {
            Assert.All(author.Books, book =>
            {
                Assert.NotEmpty(book.Categories);
                Assert.Equal(book.Categories.Count, book.Categories.Distinct(StringComparer.Ordinal).Count());
                Assert.All(book.Categories, category => Assert.Contains(category, categoryNames));
                if (book.ImageFileName is not null)
                {
                    string imagePath = Path.Combine(
                        AppContext.BaseDirectory,
                        "wwwroot",
                        "images",
                        "books",
                        "seed-assets",
                        book.ImageFileName);
                    Assert.True(File.Exists(imagePath), $"Missing book image asset: {book.ImageFileName}");
                }
            });
        }
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (isbn.Length != 13 || isbn.Any(character => !char.IsAsciiDigit(character)))
        {
            return false;
        }

        int sum = 0;
        for (int index = 0; index < 12; index++)
        {
            sum += (isbn[index] - '0') * (index % 2 == 0 ? 1 : 3);
        }

        return (10 - sum % 10) % 10 == isbn[12] - '0';
    }
}
