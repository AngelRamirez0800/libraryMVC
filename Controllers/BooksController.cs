using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly IBookQueries _bookService;
        private readonly IBookApplicationService _bookApplicationService;
        private readonly IAuthorQueries _authorService;
        private readonly ICategoryQueries _categoryService;
        private readonly IBookLifecycle _bookLifecycle;

        public BooksController(
            IBookQueries bookService,
            IBookApplicationService bookApplicationService,
            IAuthorQueries authorService,
            ICategoryQueries categoryService,
            IBookLifecycle bookLifecycle)
        {
            _bookService = bookService;
            _bookApplicationService = bookApplicationService;
            _authorService = authorService;
            _categoryService = categoryService;
            _bookLifecycle = bookLifecycle;
        }

        public async Task<IActionResult> Index()
        {
            var books = await _bookService.GetAllAsync();
            return View(books);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var book = await _bookService.GetByIdWithDetailsAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Authors = await _authorService.GetAllAsync();
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookInput book, List<Guid>? selectedAuthorIds, List<Guid>? selectedCategoryIds, ICollection<IFormFile>? images)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBookOptionsAsync(selectedCategoryIds);
                return View(new Book(book.Isbn, book.Title, book.Summary, book.IsActive));
            }

            try
            {
                await _bookApplicationService.CreateAsync(book, selectedAuthorIds, selectedCategoryIds, images);
                TempData["Success"] = "Libro creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(selectedCategoryIds), ex.Message);
                await PopulateBookOptionsAsync(selectedCategoryIds);
                return View(new Book(book.Isbn, book.Title, book.Summary, book.IsActive));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Libro creado, pero ocurrió un error con las imágenes: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var book = await _bookService.GetByIdWithDetailsAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            ViewBag.Authors = await _authorService.GetAllAsync();
            ViewBag.Categories = await _categoryService.GetAllActiveAsync();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BookInput book, List<Guid>? selectedAuthorIds, List<Guid>? selectedCategoryIds, ICollection<IFormFile>? images)
        {
            var targetId = id != Guid.Empty ? id : book.Id;
            if (targetId == Guid.Empty)
            {
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
                    return NotFound();
                }

                TempData["Success"] = images != null && images.Count > 0
                    ? $"Libro, autores e imágenes ({images.Count}) actualizados correctamente."
                    : "Libro y autores actualizados correctamente.";
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(nameof(selectedCategoryIds), ex.Message);
                var existingBook = await _bookService.GetByIdWithDetailsAsync(targetId);
                if (existingBook == null)
                {
                    return NotFound();
                }

                existingBook.UpdateDetails(book.Isbn, book.Title, book.Summary);
                ViewBag.SelectedCategoryIds = selectedCategoryIds ?? new List<Guid>();
                await PopulateBookOptionsAsync(selectedCategoryIds);
                return View(existingBook);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Información y autores guardados, pero hubo un error con las imágenes: " + ex.Message;
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
        public async Task<IActionResult> Deactivate(Guid id)
        {
            await _bookLifecycle.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Restore(Guid id)
        {
            await _bookLifecycle.RestoreAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImages(Guid id, ICollection<IFormFile> images)
        {
            try
            {
                if (images == null || images.Count == 0)
                {
                    TempData["Error"] = "Debes seleccionar al menos una imagen.";
                    return RedirectToAction(nameof(Edit), new
                    {
                        id
                    });
                }

                await _bookApplicationService.AddImagesAsync(id, images);
                TempData["Success"] = $"Se han subido {images.Count} imagen(es) correctamente.";
                return RedirectToAction(nameof(Edit), new
                {
                    id
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al subir imágenes: {ex.Message}";
                return RedirectToAction(nameof(Edit), new
                {
                    id
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveImage(Guid imageId, Guid bookId)
        {
            try
            {
                await _bookApplicationService.RemoveImageAsync(imageId);
                TempData["Success"] = "Imagen eliminada correctamente.";
                return RedirectToAction(nameof(Edit), new
                {
                    id = bookId
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar imagen: {ex.Message}";
                return RedirectToAction(nameof(Edit), new
                {
                    id = bookId
                });
            }
        }
    }
}
