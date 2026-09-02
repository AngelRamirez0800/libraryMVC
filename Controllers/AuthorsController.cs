using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace libraryMVC.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly IAuthorsService _authorService;
        private readonly IBooksService _bookService;

        public AuthorsController(IAuthorsService authorService, IBooksService bookService)
        {
            _authorService = authorService;
            _bookService = bookService;
        }

        public async Task<IActionResult> Index()
        {
            var authors = await _authorService.GetAllAsync();
            return View(authors);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var author = await _authorService.GetByIdWithBooksAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Books = await _bookService.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Author author, List<Guid>? selectedBookIds)
        {
            if (ModelState.IsValid)
            {
                await _authorService.CreateAsync(author, selectedBookIds);
                TempData["Success"] = "Autor creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Books = await _bookService.GetAllAsync();
            return View(author);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var author = await _authorService.GetByIdWithBooksAsync(id);
            if (author == null) return NotFound();

            ViewBag.Books = await _bookService.GetAllAsync();
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Author author, List<Guid>? selectedBookIds)
        {
            var targetId = id != Guid.Empty ? id : author.Id;
            if (targetId == Guid.Empty) return NotFound();

            var updatedAuthor = await _authorService.UpdateAsync(
                targetId,
                author,
                selectedBookIds ?? new List<Guid>());
            if (updatedAuthor == null) return NotFound();

            TempData["Success"] = "Autor y libros asociados actualizados correctamente.";
            return RedirectToAction(nameof(Edit), new { id = targetId });
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            await _authorService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Restore(Guid id)
        {
            await _authorService.RestoreAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}