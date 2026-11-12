using System.Security.Cryptography;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace libraryMVC.Data.Seeding;

public sealed class LiteratureSeedService : ILiteratureSeedService
{
    private const long MaximumImageSize = 5 * 1024 * 1024;
    private const string AuthorImagesDirectory = "images/authors";
    private const string BookImagesDirectory = "images/books";
    private const string BookSeedAssetsDirectory = "images/books/seed-assets";

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public LiteratureSeedService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<LiteratureSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        ValidateCatalog();
        await InstallAuthorPortraitsAsync(cancellationToken);

        Dictionary<string, Category> categories = await GetOrCreateCategoriesAsync(cancellationToken);
        Dictionary<AuthorKey, Author> authors = await GetOrCreateAuthorsAsync(cancellationToken);
        Dictionary<string, Book> books = await GetOrCreateBooksAsync(cancellationToken);
        await EnsureRelationshipsAsync(authors, categories, books, cancellationToken);
        int bookImages = await EnsureBookImagesAsync(books, cancellationToken);

        return new LiteratureSeedResult(
            LiteratureSeedCatalog.Authors.Count,
            LiteratureSeedCatalog.Authors.Sum(author => author.Books.Count),
            LiteratureSeedCatalog.Categories.Count,
            bookImages);
    }

    private async Task<Dictionary<string, Category>> GetOrCreateCategoriesAsync(
        CancellationToken cancellationToken)
    {
        string[] names = LiteratureSeedCatalog.Categories.Select(category => category.Name).ToArray();
        List<Category> existingCategories = await _context.Categories
            .Where(category => names.Contains(category.Name))
            .ToListAsync(cancellationToken);
        Dictionary<string, Category> categories = existingCategories.ToDictionary(
            category => category.Name,
            StringComparer.OrdinalIgnoreCase);

        foreach (LiteratureCategorySeed definition in LiteratureSeedCatalog.Categories)
        {
            if (categories.ContainsKey(definition.Name))
            {
                continue;
            }

            var newCategory = new Category(definition.Name, definition.Description);
            newCategory.MarkCreated();
            _context.Categories.Add(newCategory);
            categories.Add(definition.Name, newCategory);
        }

        await _context.SaveChangesAsync(cancellationToken);
        EnsureGeneratedIds(categories.Values, category => category.Id, "category");
        return categories;
    }

    private async Task<Dictionary<AuthorKey, Author>> GetOrCreateAuthorsAsync(
        CancellationToken cancellationToken)
    {
        List<Author> existingAuthors = await _context.Authors.ToListAsync(cancellationToken);
        Dictionary<AuthorKey, Author> authors = existingAuthors
            .Where(author => LiteratureSeedCatalog.Authors.Any(definition =>
                AuthorKey.From(definition) == AuthorKey.From(author)))
            .ToDictionary(author => AuthorKey.From(author));

        foreach (LiteratureAuthorSeed definition in LiteratureSeedCatalog.Authors)
        {
            AuthorKey key = AuthorKey.From(definition);
            if (authors.ContainsKey(key))
            {
                Author existing = authors[key];
                if (!string.Equals(existing.Nationality, definition.Nationality, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Existing author '{definition.FirstName} {definition.LastName}' has a different nationality. " +
                        "Resolve the conflict before running the literature seed.");
                }

                continue;
            }

            var author = new Author(
                definition.FirstName,
                definition.LastName,
                definition.Nationality,
                definition.BirthDate);
            author.MarkCreated();
            _context.Authors.Add(author);
            authors.Add(key, author);
        }

        await _context.SaveChangesAsync(cancellationToken);
        EnsureGeneratedIds(authors.Values, author => author.Id, "author");
        return authors;
    }

    private async Task<Dictionary<string, Book>> GetOrCreateBooksAsync(
        CancellationToken cancellationToken)
    {
        string[] isbns = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .Select(book => book.Isbn)
            .ToArray();
        List<Book> existingBooks = await _context.Books
            .Where(book => isbns.Contains(book.Isbn))
            .ToListAsync(cancellationToken);
        Dictionary<string, Book> books = existingBooks.ToDictionary(
            book => book.Isbn,
            StringComparer.Ordinal);

        foreach (LiteratureBookSeed definition in LiteratureSeedCatalog.Authors.SelectMany(author => author.Books))
        {
            if (books.TryGetValue(definition.Isbn, out Book? existingBook))
            {
                if (!string.Equals(existingBook.Title, definition.Title, StringComparison.Ordinal) ||
                    !string.Equals(existingBook.Summary, definition.Summary, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Existing book with ISBN '{definition.Isbn}' has different catalog data. " +
                        "Resolve the conflict before running the literature seed.");
                }

                continue;
            }

            var book = new Book(definition.Isbn, definition.Title, definition.Summary);
            book.MarkCreated();
            _context.Books.Add(book);
            books.Add(definition.Isbn, book);
        }

        await _context.SaveChangesAsync(cancellationToken);
        EnsureGeneratedIds(books.Values, book => book.Id, "book");
        return books;
    }

    private async Task EnsureRelationshipsAsync(
        IReadOnlyDictionary<AuthorKey, Author> authors,
        IReadOnlyDictionary<string, Category> categories,
        IReadOnlyDictionary<string, Book> books,
        CancellationToken cancellationToken)
    {
        string[] isbns = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .Select(book => book.Isbn)
            .ToArray();
        List<Book> booksWithRelationships = await _context.Books
            .Where(book => isbns.Contains(book.Isbn))
            .Include(book => book.Authors)
            .Include(book => book.Categories)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        Dictionary<string, LiteratureAuthorSeed> bookAuthors = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books.Select(book => (book.Isbn, Author: author)))
            .ToDictionary(item => item.Isbn, item => item.Author, StringComparer.Ordinal);
        Dictionary<string, LiteratureBookSeed> definitions = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .ToDictionary(book => book.Isbn, StringComparer.Ordinal);

        foreach (Book book in booksWithRelationships)
        {
            LiteratureBookSeed definition = definitions[book.Isbn];
            LiteratureAuthorSeed authorDefinition = bookAuthors[book.Isbn];
            Author author = authors[AuthorKey.From(authorDefinition)];
            HashSet<Guid> authorIds = book.Authors.Select(relatedAuthor => relatedAuthor.Id).ToHashSet();
            if (authorIds.Add(author.Id))
            {
                book.ReplaceAuthors(book.Authors.Append(author).ToArray());
            }

            HashSet<Guid> categoryIds = book.Categories.Select(category => category.Id).ToHashSet();
            Category[] relatedCategories = definition.Categories
                .Select(categoryName => categories[categoryName])
                .Where(category => categoryIds.Add(category.Id))
                .ToArray();
            if (relatedCategories.Length > 0)
            {
                book.ReplaceCategories(book.Categories.Concat(relatedCategories).ToArray());
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (Book book in books.Values)
        {
            if (book.Id == Guid.Empty)
            {
                throw new InvalidOperationException($"Book '{book.Isbn}' has no database-generated ID.");
            }
        }
    }

    private async Task<int> EnsureBookImagesAsync(
        IReadOnlyDictionary<string, Book> books,
        CancellationToken cancellationToken)
    {
        int imageCount = 0;
        string[] isbns = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .Where(book => book.ImageFileName is not null)
            .Select(book => book.Isbn)
            .ToArray();
        List<Book> booksWithImages = await _context.Books
            .Where(book => isbns.Contains(book.Isbn))
            .Include(book => book.Images)
            .ToListAsync(cancellationToken);

        foreach (LiteratureBookSeed definition in LiteratureSeedCatalog.Authors
                     .SelectMany(author => author.Books)
                     .Where(book => book.ImageFileName is not null))
        {
            Book book = books[definition.Isbn];
            Book trackedBook = booksWithImages.Single(entity => entity.Id == book.Id);
            string assetFileName = definition.ImageFileName!;
            string sourcePath = GetSourceAssetPath(BookSeedAssetsDirectory, assetFileName);
            ValidateJpeg(sourcePath);

            string storedFileName = $"seed-{definition.Isbn}.jpg";
            string imagePath = $"{BookImagesDirectory}/{book.Id:D}/{storedFileName}";
            string targetPath = Path.Combine(
                _environment.WebRootPath,
                BookImagesDirectory.Replace('/', Path.DirectorySeparatorChar),
                book.Id.ToString("D"),
                storedFileName);
            long fileSize = await InstallFileAsync(
                sourcePath,
                targetPath,
                cancellationToken,
                replaceExisting: true);

            BookImage? existingImage = await _context.BookImages
                .SingleOrDefaultAsync(image => image.ImagePath == imagePath, cancellationToken);
            if (existingImage is not null)
            {
                if (existingImage.BookId != book.Id)
                {
                    throw new InvalidOperationException(
                        $"Existing seeded image metadata for ISBN '{definition.Isbn}' does not match its asset.");
                }

                if (existingImage.FileSize != fileSize ||
                    !string.Equals(existingImage.OriginalFileName, storedFileName, StringComparison.Ordinal))
                {
                    existingImage.UpdateFileMetadata(storedFileName, fileSize);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                imageCount++;
                continue;
            }

            int imageNumber = trackedBook.Images.Count == 0
                ? 1
                : trackedBook.Images.Max(image => image.ImageNumber) + 1;
            var bookImage = new BookImage(
                book.Id,
                imagePath,
                imageNumber,
                storedFileName,
                fileSize);
            trackedBook.AddImage(bookImage);
            _context.BookImages.Add(bookImage);
            await _context.SaveChangesAsync(cancellationToken);
            EnsureGeneratedIds([bookImage], image => image.Id, "book image");
            imageCount++;
        }

        return imageCount;
    }

    private async Task InstallAuthorPortraitsAsync(CancellationToken cancellationToken)
    {
        foreach (LiteratureAuthorSeed author in LiteratureSeedCatalog.Authors)
        {
            string sourcePath = GetSourceAssetPath(AuthorImagesDirectory, author.PortraitFileName);
            ValidateJpeg(sourcePath);
            string targetPath = Path.Combine(
                _environment.WebRootPath,
                AuthorImagesDirectory.Replace('/', Path.DirectorySeparatorChar),
                author.PortraitFileName);
            await InstallFileAsync(sourcePath, targetPath, cancellationToken, replaceExisting: true);
        }
    }

    private string GetSourceAssetPath(string relativeDirectory, string fileName) =>
        Path.Combine(
            _environment.ContentRootPath,
            "wwwroot",
            relativeDirectory.Replace('/', Path.DirectorySeparatorChar),
            fileName);

    private static async Task<long> InstallFileAsync(
        string sourcePath,
        string targetPath,
        CancellationToken cancellationToken,
        bool replaceExisting = false)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("A required literature seed image asset is missing.", sourcePath);
        }

        FileInfo sourceInfo = new(sourcePath);
        if (sourceInfo.Length == 0 || sourceInfo.Length > MaximumImageSize)
        {
            throw new InvalidDataException(
                $"Seed image '{Path.GetFileName(sourcePath)}' must be between 1 byte and {MaximumImageSize} bytes.");
        }

        if (PathsEqual(sourcePath, targetPath))
        {
            return sourceInfo.Length;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        if (File.Exists(targetPath))
        {
            if (await ImagesMatchAsync(sourcePath, targetPath, cancellationToken))
            {
                return new FileInfo(targetPath).Length;
            }

            if (!replaceExisting)
            {
                throw new InvalidDataException(
                    $"Seed image target '{targetPath}' already exists with different contents.");
            }

            return await ReplaceExistingFileAsync(sourcePath, targetPath, cancellationToken);
        }

        bool createdTarget = false;
        try
        {
            await using FileStream source = new(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using FileStream target = new(
                targetPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            createdTarget = true;
            await source.CopyToAsync(target, cancellationToken);
            await target.FlushAsync(cancellationToken);
        }
        catch (IOException) when (!createdTarget && File.Exists(targetPath))
        {
            await EnsureSameImageAsync(sourcePath, targetPath, cancellationToken);
        }
        catch
        {
            if (createdTarget && File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            throw;
        }

        return new FileInfo(targetPath).Length;
    }

    private static async Task<long> ReplaceExistingFileAsync(
        string sourcePath,
        string targetPath,
        CancellationToken cancellationToken)
    {
        string temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (FileStream source = new(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (FileStream temporary = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(temporary, cancellationToken);
                await temporary.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, targetPath, overwrite: true);
            return new FileInfo(targetPath).Length;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task<bool> ImagesMatchAsync(
        string sourcePath,
        string targetPath,
        CancellationToken cancellationToken)
    {
        await using FileStream source = File.OpenRead(sourcePath);
        await using FileStream target = File.OpenRead(targetPath);
        byte[] sourceHash = await SHA256.HashDataAsync(source, cancellationToken);
        byte[] targetHash = await SHA256.HashDataAsync(target, cancellationToken);
        return CryptographicOperations.FixedTimeEquals(sourceHash, targetHash);
    }

    private static async Task EnsureSameImageAsync(
        string sourcePath,
        string targetPath,
        CancellationToken cancellationToken)
    {
        if (!await ImagesMatchAsync(sourcePath, targetPath, cancellationToken))
        {
            throw new InvalidDataException(
                $"Seed image target '{targetPath}' already exists with different contents.");
        }
    }

    private static void ValidateJpeg(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("A required literature seed image asset is missing.", path);
        }

        FileInfo file = new(path);
        if (file.Length < 3 || file.Length > MaximumImageSize)
        {
            throw new InvalidDataException(
                $"Seed image '{Path.GetFileName(path)}' must be between 3 bytes and {MaximumImageSize} bytes.");
        }

        using FileStream stream = File.OpenRead(path);
        Span<byte> signature = stackalloc byte[3];
        if (stream.Read(signature) != signature.Length ||
            signature[0] != 0xff ||
            signature[1] != 0xd8 ||
            signature[2] != 0xff)
        {
            throw new InvalidDataException($"Seed image '{Path.GetFileName(path)}' is not a JPEG file.");
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static void EnsureGeneratedIds<T>(
        IEnumerable<T> entities,
        Func<T, Guid> getId,
        string entityName)
    {
        if (entities.Any(entity => getId(entity) == Guid.Empty))
        {
            throw new InvalidOperationException(
                $"SQL Server did not generate an ID for a seeded {entityName}.");
        }
    }

    private static void ValidateCatalog()
    {
        if (LiteratureSeedCatalog.Authors.Count != 3 ||
            LiteratureSeedCatalog.Authors.Any(author => author.Books.Count < 7))
        {
            throw new InvalidOperationException("The literature seed must contain three authors and at least seven books each.");
        }

        string[] allIsbns = LiteratureSeedCatalog.Authors
            .SelectMany(author => author.Books)
            .Select(book => book.Isbn)
            .ToArray();
        if (allIsbns.Distinct(StringComparer.Ordinal).Count() != allIsbns.Length)
        {
            throw new InvalidOperationException("The literature seed contains duplicate ISBN values.");
        }

        foreach (string isbn in allIsbns)
        {
            if (!IsValidIsbn13(isbn))
            {
                throw new InvalidOperationException($"Seed ISBN '{isbn}' has an invalid ISBN-13 check digit.");
            }
        }

        HashSet<string> categoryNames = LiteratureSeedCatalog.Categories
            .Select(category => category.Name)
            .ToHashSet(StringComparer.Ordinal);
        foreach (LiteratureBookSeed book in LiteratureSeedCatalog.Authors.SelectMany(author => author.Books))
        {
            if (book.Categories.Count == 0 ||
                book.Categories.Any(category => !categoryNames.Contains(category)))
            {
                throw new InvalidOperationException(
                    $"Book '{book.Title}' references a missing or invalid seed category.");
            }
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

    private readonly record struct AuthorKey(string FirstName, string LastName, DateTime BirthDate)
    {
        public static AuthorKey From(LiteratureAuthorSeed author) =>
            new(Normalize(author.FirstName), Normalize(author.LastName), author.BirthDate.Date);

        public static AuthorKey From(Author author) =>
            new(Normalize(author.FirstName), Normalize(author.LastName), author.BirthDate.Date);

        private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    }
}
