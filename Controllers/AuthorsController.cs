using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace libraryMVC.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly AuthorsService _authorService;

        public AuthorsController(AuthorsService authorService)
        {
            _authorService = authorService;
        }

        public async Task<IActionResult> Index()
        {
            var authors = await _authorService.GetAllAsync();
            return View(authors);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var author = await _authorService.GetByIdAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Author author)
        {
            if (ModelState.IsValid)
            {
                author.CreatedAt = DateTime.Now;
                await _authorService.AddAsync(author);
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var author = await _authorService.GetByIdAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Author author)
        {
            var targetId = id != Guid.Empty ? id : author.Id;
            if (targetId == Guid.Empty) return NotFound();

            var existingAuthor = await _authorService.GetByIdAsync(targetId);
            if (existingAuthor == null) return NotFound();

            existingAuthor.FirstName = author.FirstName;
            existingAuthor.LastName = author.LastName;
            existingAuthor.Nationality = author.Nationality;
            existingAuthor.BirthDate = author.BirthDate;

            if (author.IsActive)
            {
                existingAuthor.Activate();
            }
            else
            {
                existingAuthor.Deactivate();
            }

            await _authorService.UpdateSync(existingAuthor);
            return RedirectToAction(nameof(Index));
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