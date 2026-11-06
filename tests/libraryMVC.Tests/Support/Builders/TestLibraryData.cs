using Bogus;
using libraryMVC.Models;

namespace libraryMVC.Tests.Support.Builders;

public sealed record TestLibraryData(
    IReadOnlyList<Author> Authors,
    IReadOnlyList<Book> Books,
    IReadOnlyList<Category> Categories)
{
    public const int Seed = 1337;

    public static TestLibraryData Create()
    {
        Faker faker = new("es");
        faker.Random = new Randomizer(Seed);

        List<Author> authors = Enumerable.Range(0, 2)
            .Select(_ => new Author(
                faker.Name.FirstName(),
                faker.Name.LastName(),
                faker.Address.Country(),
                faker.Date.Between(new DateTime(1940, 1, 1), new DateTime(2000, 1, 1))))
            .ToList();

        List<Category> categories = Enumerable.Range(0, 5)
            .Select(index => new Category($"Categoría {index + 1}: {faker.Commerce.Categories(1)[0]}", faker.Lorem.Sentence()))
            .ToList();

        List<Book> books = Enumerable.Range(0, 10)
            .Select(index => new Book(
                $"978-0-{index + 1:D4}-{100000 + index:D6}",
                faker.Commerce.ProductName(),
                faker.Lorem.Paragraph()))
            .ToList();

        for (int index = 0; index < books.Count; index++)
        {
            Book book = books[index];
            Author[] bookAuthors = index < 5
                ? [authors[0]]
                : [authors[1]];

            if (index is 4 or 5)
            {
                bookAuthors = authors.ToArray();
            }

            Category[] bookCategories =
            [
                categories[index % categories.Count],
                categories[(index + 1) % categories.Count]
            ];

            book.ReplaceAuthors(bookAuthors);
            book.ReplaceCategories(bookCategories);
        }

        foreach (Author author in authors)
        {
            author.ReplaceBooks(books.Where(book => book.Authors.Contains(author)));
        }

        foreach (Category category in categories)
        {
            category.ReplaceBooks(books.Where(book => book.Categories.Contains(category)));
        }

        return new TestLibraryData(authors, books, categories);
    }
}
