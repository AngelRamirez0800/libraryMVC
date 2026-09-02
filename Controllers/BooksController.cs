using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace libraryMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly IBooksService _bookService;
        private readonly IBookApplicationService _bookApplicationService;
        private readonly IAuthorsService _authorService;

        public BooksController(
            IBooksService bookService,
            IBookApplicationService bookApplicationService,
            IAuthorsService authorService)
        {
            _bookService = bookService;
            _bookApplicationService = bookApplicationService;
            _authorService = authorService;
        }

        public async Task<IActionResult> Index()
        {
            var books = await _bookService.GetAllAsync();
            return View(books);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var book = await _bookService.GetByIdWithDetailsAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Authors = await _authorService.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, List<Guid>? selectedAuthorIds, ICollection<IFormFile>? images)
        {
            if (ModelState.IsValid)
            {
                if (images != null && images.Count > 0)
                {
                    try
                    {
                        await _bookApplicationService.CreateAsync(book, selectedAuthorIds, images);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Libro creado, pero ocurrió un error con las imágenes: {ex.Message}";
                        return RedirectToAction(nameof(Edit), new { id = book.Id });
                    }
                }
                else
                {
                    await _bookApplicationService.CreateAsync(book, selectedAuthorIds, null);
                }

                TempData["Success"] = "Libro creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Authors = await _authorService.GetAllAsync();
            return View(book);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var book = await _bookService.GetByIdWithDetailsAsync(id);
            if (book == null) return NotFound();
            ViewBag.Authors = await _authorService.GetAllAsync();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Book book, List<Guid>? selectedAuthorIds, ICollection<IFormFile>? images)
        {
            var targetId = id != Guid.Empty ? id : book.Id;
            if (targetId == Guid.Empty) return NotFound();

            if (images != null && images.Count > 0)
            {
                try
                {
                    var updatedBook = await _bookApplicationService.UpdateAsync(
                        targetId,
                        book,
                        selectedAuthorIds ?? new List<Guid>(),
                        images);
                    if (updatedBook == null) return NotFound();
                    TempData["Success"] = $"Libro, autores e imágenes ({images.Count}) actualizados correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Información y autores guardados, pero hubo un error con las imágenes: {ex.Message}";
                }
            }
            else
            {
                var updatedBook = await _bookApplicationService.UpdateAsync(
                    targetId,
                    book,
                    selectedAuthorIds ?? new List<Guid>(),
                    null);
                if (updatedBook == null) return NotFound();
                TempData["Success"] = "Libro y autores actualizados correctamente.";
            }

            return RedirectToAction(nameof(Edit), new { id = targetId });
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            await _bookService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Restore(Guid id)
        {
            await _bookService.RestoreAsync(id);
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
                    return RedirectToAction(nameof(Edit), new { id });
                }

                await _bookApplicationService.AddImagesAsync(id, images);
                TempData["Success"] = $"Se han subido {images.Count} imagen(es) correctamente.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al subir imágenes: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
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
                return RedirectToAction(nameof(Edit), new { id = bookId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar imagen: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id = bookId });
            }
        }
    }
}