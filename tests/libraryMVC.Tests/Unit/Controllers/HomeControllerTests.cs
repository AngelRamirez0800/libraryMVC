using libraryMVC.Controllers;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Tests.Support.Builders;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace libraryMVC.Tests.Unit.Controllers;

public sealed class HomeControllerTests
{
    [Fact]
    public async Task IndexReturnsFeaturedBooksFromTheQueryService()
    {
        List<Book> books = TestLibraryData.Create().Books.Take(3).ToList();
        var queries = new Mock<IBookQueries>();
        queries.Setup(service => service.GetFeaturedBooksAsync(3)).ReturnsAsync(books);
        var controller = new HomeController(queries.Object);

        IActionResult result = await controller.Index();

        ViewResult view = Assert.IsType<ViewResult>(result);
        Assert.Equal(books, view.Model);
        queries.Verify(service => service.GetFeaturedBooksAsync(3), Times.Once);
    }
}
