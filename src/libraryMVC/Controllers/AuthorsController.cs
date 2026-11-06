using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libraryMVC.DTOs;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers
{
    [Authorize]
    public class AuthorsController : Controller
    {
        private readonly IAuthorQueries _authorQueries;
        private readonly IAuthorApplicationService _authorApplication;
        private readonly IAuthorLifecycle _authorLifecycle;
        private readonly IBookQueries _bookQueries;
        private readonly ILogger<AuthorsController> _logger;

        public AuthorsController(
            IAuthorQueries authorQueries,
            IAuthorApplicationService authorApplication,
            IAuthorLifecycle authorLifecycle,
            IBookQueries bookQueries,
            ILogger<AuthorsController> logger)
        {
            _authorQueries = authorQueries;
            _authorApplication = authorApplication;
            _authorLifecycle = authorLifecycle;
            _bookQueries = bookQueries;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Retrieving the author list.");
            var authors = await _authorQueries.GetAllAsync();
            return View(authors);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            _logger.LogInformation("Retrieving author details for {AuthorId}.", id);
            var author = await _authorQueries.GetByIdWithBooksAsync(id);
            if (author == null)
            {
                _logger.LogWarning("Author {AuthorId} was not found.", id);
                return NotFound();
            }

            return View(author);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Displaying the author creation form.");
            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorInput author, List<Guid>? selectedBookIds)
        {
            _logger.LogInformation("Processing an author creation request.");
            if (ModelState.IsValid)
            {
                await _authorApplication.CreateAsync(author, selectedBookIds);
                _logger.LogInformation("Author was created successfully.");
                TempData["Success"] = "Autor creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View(new Author(author.FirstName, author.LastName, author.Nationality, author.BirthDate, author.IsActive));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            _logger.LogInformation("Retrieving author {AuthorId} for editing.", id);
            var author = await _authorQueries.GetByIdWithBooksAsync(id);
            if (author == null)
            {
                _logger.LogWarning("Author {AuthorId} was not found for editing.", id);
                return NotFound();
            }

            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View(author);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AuthorInput author, List<Guid>? selectedBookIds)
        {
            _logger.LogInformation("Processing an author update request for {AuthorId}.", id);
            var targetId = id != Guid.Empty ? id : author.Id;
            if (targetId == Guid.Empty)
            {
                _logger.LogWarning("Author update was rejected because no valid author ID was provided.");
                return NotFound();
            }

            var updatedAuthor = await _authorApplication.UpdateAsync(
                targetId,
                author,
                selectedBookIds ?? new List<Guid>());
            if (updatedAuthor == null)
            {
                _logger.LogWarning("Author {AuthorId} was not found for update.", targetId);
                return NotFound();
            }

            _logger.LogInformation("Author {AuthorId} was updated successfully.", targetId);
            TempData["Success"] = "Autor y libros asociados actualizados correctamente.";
            return RedirectToAction(nameof(Edit), new
            {
                id = targetId
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            _logger.LogInformation("Processing author deactivation for {AuthorId}.", id);
            if (await _authorLifecycle.DeleteAsync(id))
            {
                _logger.LogInformation("Author {AuthorId} was deactivated successfully.", id);
            }
            else
            {
                var author = await _authorQueries.GetByIdWithBooksAsync(id);
                if (author == null)
                {
                    _logger.LogWarning("Author {AuthorId} was not found for deactivation.", id);
                }
                else if (!author.IsActive)
                {
                    _logger.LogInformation("Author {AuthorId} was inactive when the deactivation result was checked; this request made no state change.", id);
                }
                else
                {
                    _logger.LogWarning("Author {AuthorId} remained active after the deactivation request.", id);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(Guid id)
        {
            _logger.LogInformation("Processing author restoration for {AuthorId}.", id);
            if (await _authorLifecycle.RestoreAsync(id))
            {
                _logger.LogInformation("Author {AuthorId} was restored successfully.", id);
            }
            else
            {
                var author = await _authorQueries.GetByIdWithBooksAsync(id);
                if (author == null)
                {
                    _logger.LogWarning("Author {AuthorId} was not found for restoration.", id);
                }
                else if (author.IsActive)
                {
                    _logger.LogInformation("Author {AuthorId} was active when the restoration result was checked; this request made no state change.", id);
                }
                else
                {
                    _logger.LogWarning("Author {AuthorId} remained inactive after the restoration request.", id);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
