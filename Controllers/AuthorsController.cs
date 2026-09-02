using libraryMVC.Interfaces;
using libraryMVC.DTOs;
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
        private readonly IAuthorQueries _authorQueries;
        private readonly IAuthorApplicationService _authorApplication;
        private readonly IAuthorLifecycle _authorLifecycle;
        private readonly IBookQueries _bookQueries;

        public AuthorsController(
            IAuthorQueries authorQueries,
            IAuthorApplicationService authorApplication,
            IAuthorLifecycle authorLifecycle,
            IBookQueries bookQueries)
        {
            _authorQueries = authorQueries;
            _authorApplication = authorApplication;
            _authorLifecycle = authorLifecycle;
            _bookQueries = bookQueries;
        }

        public async Task<IActionResult> Index()
        {
            var authors = await _authorQueries.GetAllAsync();
            return View(authors);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var author = await _authorQueries.GetByIdWithBooksAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorInput author, List<Guid>? selectedBookIds)
        {
            if (ModelState.IsValid)
            {
                await _authorApplication.CreateAsync(author, selectedBookIds);
                TempData["Success"] = "Autor creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View(new Author(author.FirstName, author.LastName, author.Nationality, author.BirthDate, author.IsActive));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var author = await _authorQueries.GetByIdWithBooksAsync(id);
            if (author == null) return NotFound();

            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AuthorInput author, List<Guid>? selectedBookIds)
        {
            var targetId = id != Guid.Empty ? id : author.Id;
            if (targetId == Guid.Empty) return NotFound();

            var updatedAuthor = await _authorApplication.UpdateAsync(
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
            await _authorLifecycle.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Restore(Guid id)
        {
            await _authorLifecycle.RestoreAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}