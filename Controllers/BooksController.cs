using libraryMVC.Interfaces;
using libraryMVC.Models;
using libraryMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace libraryMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly BooksService _bookService;

        public BooksController(BooksService bookService)
        {
            _bookService = bookService;
        }

        public async Task<IActionResult> Index()
        {
            var books = await _bookService.GetAllAsync();
            return View(books);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var book = await _bookService.GetByIdAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (ModelState.IsValid)
            {
                book.CreatedAt = DateTime.Now;
                await _bookService.AddAsync(book);
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var book = await _bookService.GetByIdAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Book book)
        {
            var targetId = id != Guid.Empty ? id : book.Id;
            if (targetId == Guid.Empty) return NotFound();

            var existingBook = await _bookService.GetByIdAsync(targetId);
            if (existingBook == null) return NotFound();

            existingBook.Isbn = book.Isbn;
            existingBook.Title = book.Title;
            existingBook.Summary = book.Summary;
            
            if (book.IsActive)
            {
                existingBook.Activate();
            }
            else
            {
                existingBook.Deactivate();
            }

            await _bookService.UpdateSync(existingBook);
            return RedirectToAction(nameof(Index));
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
    }
}