using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.DTOs;
using libraryMVC.DTOs.Lists;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Security;
using libraryMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly IBookQueries _bookService;
        private readonly IBookApplicationService _bookApplicationService;
        private readonly IAuthorQueries _authorService;
        private readonly ICategoryQueries _categoryService;
        private readonly IBookLifecycle _bookLifecycle;
        private readonly ILogger<BooksController> _logger;

        public BooksController(
            IBookQueries bookService,
            IBookApplicationService bookApplicationService,
            IAuthorQueries authorService,
            ICategoryQueries categoryService,
            IBookLifecycle bookLifecycle,
            ILogger<BooksController> logger)
        {
            _bookService = bookService;
            _bookApplicationService = bookApplicationService;
            _authorService = authorService;
            _categoryService = categoryService;
            _bookLifecycle = bookLifecycle;
            _logger = logger;
        }

        [Authorize(Roles = RoleNames.BookReaders)]
        public async Task<IActionResult> Index(
            [FromQuery] BookListFilter filters,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving the book list.");
            PageSlice<BookListItem> results = await _bookService.GetPageAsync(
                filters,
                1,
                ListPageDefaults.PageSize,
                cancellationToken);
            BookFilterOptions options = await _bookService.GetFilterOptionsAsync(cancellationToken);
            return View(new BooksIndexViewModel(results, filters, options));
        }

        [Authorize(Roles = RoleNames.BookReaders)]
        [HttpGet]
        public async Task<IActionResult> LoadMore(
            [FromQuery] BookListFilter filters,
            int page,
            CancellationToken cancellationToken)
        {
            PageSlice<BookListItem> results = await _bookService.GetPageAsync(
                filters,
                page,
                ListPageDefaults.PageSize,
                cancellationToken);
            return PartialView("_Rows", results);
        }

        [Authorize(Roles = RoleNames.BookReaders)]
        public async Task<IActionResult> Details(Guid id)
        {
            _logger.LogInformation("Retrieving book details for {BookId}.", id);
            var book = await _bookService.GetByIdWithDetailsAsync(id);
            if (book == null)
            {
                _logger.LogWarning("Book {BookId} was not found.", id);
                return NotFound();
            }

            return View(book);
        }

        [Authorize(Roles = RoleNames.BookManagers)]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Displaying the book creation form.");
            ViewBag.Authors = await _authorService.GetAllAsync();
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.BookManagers)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookInput book, List<Guid>? selectedAuthorIds, List<Guid>? selectedCategoryIds, ICollection<IFormFile>? images)
        {
            _logger.LogInformation("Processing a book creation request.");
            if (!ModelState.IsValid)
            {
                await PopulateBookOptionsAsync(selectedCategoryIds);
                return View(new Book(book.Isbn, book.Title, book.Summary, book.IsActive));
            }

            try
            {
                await _bookApplicationService.CreateAsync(book, selectedAuthorIds, selectedCategoryIds, images);
                _logger.LogInformation("Book was created successfully.");
                TempData["Success"] = "Book created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Book creation was rejected by input validation.");
                ModelState.AddModelError(nameof(selectedCategoryIds), ex.Message);
                await PopulateBookOptionsAsync(selectedCategoryIds);
                return View(new Book(book.Isbn, book.Title, book.Summary, book.IsActive));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing a newly created book.");
                TempData["Error"] = "Book creation could not be completed. Some changes may have been saved; check the book list before retrying.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = RoleNames.BookManagers)]
        public async Task<IActionResult> Edit(Guid id)
        {
            _logger.LogInformation("Retrieving book {BookId} for editing.", id);
            var book = await _bookService.GetByIdWithDetailsAsync(id);
            if (book == null)
            {
                _logger.LogWarning("Book {BookId} was not found for editing.", id);
                return NotFound();
            }

            ViewBag.Authors = await _authorService.GetAllAsync();
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            return View(book);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.BookManagers)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BookInput book, List<Guid>? selectedAuthorIds, List<Guid>? selectedCategoryIds, ICollection<IFormFile>? images)
        {
            _logger.LogInformation("Processing a book update request for {BookId}.", id);
            var targetId = id != Guid.Empty ? id : book.Id;
            if (targetId == Guid.Empty)
            {
                _logger.LogWarning("Book update was rejected because no valid book ID was provided.");
                return NotFound();
            }

            try
            {
                var updatedBook = await _bookApplicationService.UpdateAsync(
                    targetId,
                    book,
                    selectedAuthorIds ?? new List<Guid>(),
                    selectedCategoryIds ?? new List<Guid>(),
                    images);
                if (updatedBook == null)
                {
                    _logger.LogWarning("Book {BookId} was not found for update.", targetId);
                    return NotFound();
                }

                _logger.LogInformation("Book {BookId} was updated successfully.", targetId);
                TempData["Success"] = images != null && images.Count > 0
                    ? $"Book, authors, and images ({images.Count}) updated successfully."
                    : "Book and authors updated successfully.";
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Book update for {BookId} was rejected by input validation.", targetId);
                ModelState.AddModelError(nameof(selectedCategoryIds), ex.Message);
                var existingBook = await _bookService.GetByIdWithDetailsAsync(targetId);
                if (existingBook == null)
                {
                    _logger.LogWarning("Book {BookId} was not found while returning validation feedback.", targetId);
                    return NotFound();
                }

                existingBook.UpdateDetails(book.Isbn, book.Title, book.Summary);
                ViewBag.SelectedCategoryIds = selectedCategoryIds ?? new List<Guid>();
                await PopulateBookOptionsAsync(selectedCategoryIds);
                return View(existingBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating book {BookId}.", targetId);
                TempData["Error"] = "Book update could not be completed. Some changes may have been saved; review the book before retrying.";
            }

            return RedirectToAction(nameof(Edit), new
            {
                id = targetId
            });
        }

        private async Task PopulateBookOptionsAsync(IEnumerable<Guid>? selectedCategoryIds = null)
        {
            ViewBag.Authors = await _authorService.GetAllAsync();
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            ViewBag.SelectedCategoryIds = selectedCategoryIds ?? Enumerable.Empty<Guid>();
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.BookManagers)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            _logger.LogInformation("Processing book deactivation for {BookId}.", id);
            if (await _bookLifecycle.DeleteAsync(id))
            {
                _logger.LogInformation("Book {BookId} was deactivated successfully.", id);
            }
            else
            {
                var book = await _bookService.GetByIdWithDetailsAsync(id);
                if (book == null)
                {
                    _logger.LogWarning("Book {BookId} was not found for deactivation.", id);
                }
                else if (!book.IsActive)
                {
                    _logger.LogInformation("Book {BookId} was inactive when the deactivation result was checked; this request made no state change.", id);
                }
                else
                {
                    _logger.LogWarning("Book {BookId} remained active after the deactivation request.", id);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.BookManagers)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(Guid id)
        {
            _logger.LogInformation("Processing book restoration for {BookId}.", id);
            if (await _bookLifecycle.RestoreAsync(id))
            {
                _logger.LogInformation("Book {BookId} was restored successfully.", id);
            }
            else
            {
                var book = await _bookService.GetByIdWithDetailsAsync(id);
                if (book == null)
                {
                    _logger.LogWarning("Book {BookId} was not found for restoration.", id);
                }
                else if (book.IsActive)
                {
                    _logger.LogInformation("Book {BookId} was active when the restoration result was checked; this request made no state change.", id);
                }
                else
                {
                    _logger.LogWarning("Book {BookId} remained inactive after the restoration request.", id);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.BookManagers)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImages(Guid id, ICollection<IFormFile> images)
        {
            _logger.LogInformation("Processing image upload for book {BookId}.", id);
            try
            {
                if (images == null || images.Count == 0)
                {
                    _logger.LogWarning("Image upload for book {BookId} was rejected because no images were selected.", id);
                    TempData["Error"] = "You must select at least one image.";
                    return RedirectToAction(nameof(Edit), new
                    {
                        id
                    });
                }

                await _bookApplicationService.AddImagesAsync(id, images);
                _logger.LogInformation("Image upload completed for book {BookId}.", id);
                TempData["Success"] = $"{images.Count} image(s) uploaded successfully.";
                return RedirectToAction(nameof(Edit), new
                {
                    id
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Image upload for book {BookId} was rejected by input validation.", id);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Edit), new
                {
                    id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while uploading images for book {BookId}.", id);
                TempData["Error"] = "An error occurred while uploading images. Please try again or contact support.";
                return RedirectToAction(nameof(Edit), new
                {
                    id
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.BookManagers)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveImage(Guid imageId, Guid bookId)
        {
            _logger.LogInformation("Processing image removal for book {BookId}.", bookId);
            try
            {
                await _bookApplicationService.RemoveImageAsync(imageId);
                _logger.LogInformation("Image removal completed for book {BookId}.", bookId);
                TempData["Success"] = "Image deleted successfully.";
                return RedirectToAction(nameof(Edit), new
                {
                    id = bookId
                });
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Image {ImageId} was not found for removal from book {BookId}.", imageId, bookId);
                TempData["Error"] = "The image could not be found. Refresh the page and try again.";
                return RedirectToAction(nameof(Edit), new
                {
                    id = bookId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while removing an image from book {BookId}.", bookId);
                TempData["Error"] = "An error occurred while deleting the image. Please try again or contact support.";
                return RedirectToAction(nameof(Edit), new
                {
                    id = bookId
                });
            }
        }
    }
}
