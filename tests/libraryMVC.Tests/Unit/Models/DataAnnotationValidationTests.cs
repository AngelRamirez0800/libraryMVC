using System.ComponentModel.DataAnnotations;
using libraryMVC.DTOs;
using libraryMVC.Models;
using Xunit;

namespace libraryMVC.Tests.Unit.Models;

public sealed class DataAnnotationValidationTests
{
    [Fact]
    public void AuthorValidatesRequiredFieldsAndMaximumLengths()
    {
        Author author = new();
        author.UpdateDetails(new string('a', 101), string.Empty, string.Empty, DateTime.Today);

        IReadOnlyList<ValidationResult> errors = Validate(author);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Author.FirstName)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Author.LastName)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Author.Nationality)));
    }

    [Fact]
    public void BookValidatesRequiredFieldsAndMaximumLengths()
    {
        Book book = new();
        book.UpdateDetails(new string('i', 18), new string('t', 201), new string('s', 4001));

        IReadOnlyList<ValidationResult> errors = Validate(book);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Book.Isbn)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Book.Title)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Book.Summary)));
    }

    [Fact]
    public void CategoryValidatesRequiredNameAndOptionalDescriptionLength()
    {
        Category category = new();
        category.UpdateDetails(string.Empty, new string('d', 251));

        IReadOnlyList<ValidationResult> errors = Validate(category);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Category.Name)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(Category.Description)));
    }

    [Fact]
    public void BookImageValidatesPositiveNumberAndSizeAndStringLengths()
    {
        BookImage image = new(Guid.NewGuid(), new string('p', 501), 0, new string('f', 256), 0);

        IReadOnlyList<ValidationResult> errors = Validate(image);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(BookImage.ImagePath)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(BookImage.ImageNumber)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(BookImage.OriginalFileName)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(BookImage.FileSize)));
    }

    [Fact]
    public void DefaultLoginAndRegistrationModelsRequireCredentials()
    {
        IReadOnlyList<ValidationResult> loginErrors = Validate(new LoginViewModel());
        IReadOnlyList<ValidationResult> registrationErrors = Validate(new RegisterViewModel());

        Assert.Contains(loginErrors, error => error.MemberNames.Contains(nameof(LoginViewModel.UserNameOrEmail)));
        Assert.Contains(loginErrors, error => error.MemberNames.Contains(nameof(LoginViewModel.Password)));
        Assert.Contains(registrationErrors, error => error.MemberNames.Contains(nameof(RegisterViewModel.UserName)));
        Assert.Contains(registrationErrors, error => error.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        Assert.Contains(registrationErrors, error => error.MemberNames.Contains(nameof(RegisterViewModel.Password)));
        Assert.Contains(registrationErrors, error => error.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    [Fact]
    public void RegistrationValidatesEmailAndMatchingPasswords()
    {
        RegisterViewModel model = new()
        {
            UserName = "reader",
            Email = "not-an-email",
            Password = "secret",
            ConfirmPassword = "different"
        };

        IReadOnlyList<ValidationResult> errors = Validate(model);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    [Fact]
    public void InputDtosEnforceRequiredFieldsAndLengthLimits()
    {
        AuthorInput author = new()
        {
            FirstName = new string('a', 101),
            LastName = string.Empty,
            Nationality = string.Empty
        };
        BookInput book = new()
        {
            Isbn = new string('i', 18),
            Title = new string('t', 201),
            Summary = new string('s', 4001)
        };
        CategoryInput category = new()
        {
            Name = string.Empty,
            Description = new string('d', 251)
        };

        IReadOnlyList<ValidationResult> authorErrors = Validate(author);
        IReadOnlyList<ValidationResult> bookErrors = Validate(book);
        IReadOnlyList<ValidationResult> categoryErrors = Validate(category);

        Assert.Contains(authorErrors, error => error.MemberNames.Contains(nameof(AuthorInput.FirstName)));
        Assert.Contains(authorErrors, error => error.MemberNames.Contains(nameof(AuthorInput.LastName)));
        Assert.Contains(authorErrors, error => error.MemberNames.Contains(nameof(AuthorInput.Nationality)));
        Assert.Contains(bookErrors, error => error.MemberNames.Contains(nameof(BookInput.Isbn)));
        Assert.Contains(bookErrors, error => error.MemberNames.Contains(nameof(BookInput.Title)));
        Assert.Contains(bookErrors, error => error.MemberNames.Contains(nameof(BookInput.Summary)));
        Assert.Contains(categoryErrors, error => error.MemberNames.Contains(nameof(CategoryInput.Name)));
        Assert.Contains(categoryErrors, error => error.MemberNames.Contains(nameof(CategoryInput.Description)));
    }

    [Fact]
    public void ModelsAndInputDtosAcceptValuesAtTheirLengthBoundaries()
    {
        Author author = new(new string('a', 100), new string('b', 100), new string('n', 100), DateTime.Today);
        Book book = new(new string('i', 17), new string('t', 200), new string('s', 4000));
        Category category = new(new string('n', 100), new string('d', 250));
        AuthorInput authorInput = new()
        {
            FirstName = new string('a', 100),
            LastName = new string('b', 100),
            Nationality = new string('n', 100)
        };
        BookInput bookInput = new()
        {
            Isbn = new string('i', 17),
            Title = new string('t', 200),
            Summary = new string('s', 4000)
        };
        CategoryInput categoryInput = new()
        {
            Name = new string('n', 100),
            Description = new string('d', 250)
        };

        Assert.Empty(Validate(author));
        Assert.Empty(Validate(book));
        Assert.Empty(Validate(category));
        Assert.Empty(Validate(authorInput));
        Assert.Empty(Validate(bookInput));
        Assert.Empty(Validate(categoryInput));
    }

    private static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        List<ValidationResult> errors = new();
        Validator.TryValidateObject(instance, new ValidationContext(instance), errors, validateAllProperties: true);
        return errors;
    }
}
