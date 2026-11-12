using libraryMVC.DTOs;
using libraryMVC.DTOs.Lists;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ICategoryQueries _categoryQueries;
        private readonly ICategoryApplicationService _categoryApplication;
        private readonly ICategoryLifecycle _categoryLifecycle;
        private readonly IBookQueries _bookQueries;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            ICategoryQueries categoryQueries,
            ICategoryApplicationService categoryApplication,
            ICategoryLifecycle categoryLifecycle,
            IBookQueries bookQueries,
            ILogger<CategoryController> logger)
        {
            _categoryQueries = categoryQueries;
            _categoryApplication = categoryApplication;
            _categoryLifecycle = categoryLifecycle;
            _bookQueries = bookQueries;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            [FromQuery] CategoryListFilter filters,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving the category list.");
            PageSlice<CategoryListItem> results = await _categoryQueries.GetPageAsync(
                filters,
                1,
                ListPageDefaults.PageSize,
                cancellationToken);
            return View(new CategoriesIndexViewModel(results, filters));
        }

        [HttpGet]
        public async Task<IActionResult> LoadMore(
            [FromQuery] CategoryListFilter filters,
            int page,
            CancellationToken cancellationToken)
        {
            PageSlice<CategoryListItem> results = await _categoryQueries.GetPageAsync(
                filters,
                page,
                ListPageDefaults.PageSize,
                cancellationToken);
            return PartialView("_Rows", results);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            _logger.LogInformation("Retrieving category details for {CategoryId}.", id);
            var category = await _categoryQueries.GetByIdWithBooksAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category {CategoryId} was not found.", id);
            }

            return category == null ? NotFound() : View(category);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Displaying the category creation form.");
            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryInput category, List<Guid>? selectedBookIds)
        {
            _logger.LogInformation("Processing a category creation request.");
            if (!ModelState.IsValid)
            {
                ViewBag.Books = await _bookQueries.GetAllAsync();
                return View(new Category(category.Name, category.Description, category.IsActive));
            }

            try
            {
                var createdCategory = await _categoryApplication.CreateAsync(category, selectedBookIds);
                _logger.LogInformation("Category {CategoryId} was created successfully.", createdCategory.Id);
                TempData["Success"] = "Categoría creada correctamente.";
                return RedirectToAction(nameof(Edit), new
                {
                    id = createdCategory.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Category creation was rejected by business validation.");
                ModelState.AddModelError(nameof(category.Name), ex.Message);
                ViewBag.Books = await _bookQueries.GetAllAsync();
                return View(new Category(category.Name, category.Description, category.IsActive));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            _logger.LogInformation("Retrieving category {CategoryId} for editing.", id);
            var category = await _categoryQueries.GetByIdWithBooksAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category {CategoryId} was not found for editing.", id);
                return NotFound();
            }

            ViewBag.Books = await _bookQueries.GetAllAsync();
            return View(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CategoryInput category, List<Guid>? selectedBookIds)
        {
            _logger.LogInformation("Processing a category update request for {CategoryId}.", id);
            if (!ModelState.IsValid)
            {
                var invalidCategory = await _categoryQueries.GetByIdWithBooksAsync(id);
                if (invalidCategory == null)
                {
                    _logger.LogWarning("Category {CategoryId} was not found for validation feedback.", id);
                    return NotFound();
                }

                invalidCategory.UpdateDetails(category.Name, category.Description);
                ViewBag.Books = await _bookQueries.GetAllAsync();
                return View(invalidCategory);
            }

            try
            {
                var updatedCategory = await _categoryApplication.UpdateAsync(id, category, selectedBookIds ?? new List<Guid>());
                if (updatedCategory == null)
                {
                    _logger.LogWarning("Category {CategoryId} was not found for update.", id);
                    return NotFound();
                }

                _logger.LogInformation("Category {CategoryId} was updated successfully.", id);
                TempData["Success"] = "Categoría actualizada correctamente.";
                return RedirectToAction(nameof(Edit), new
                {
                    id
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Category update for {CategoryId} was rejected by business validation.", id);
                ModelState.AddModelError(nameof(category.Name), ex.Message);
                var invalidCategory = await _categoryQueries.GetByIdWithBooksAsync(id);
                if (invalidCategory == null)
                {
                    _logger.LogWarning("Category {CategoryId} was not found for validation feedback.", id);
                    return NotFound();
                }

                invalidCategory.UpdateDetails(category.Name, category.Description);
                ViewBag.Books = await _bookQueries.GetAllAsync();
                return View(invalidCategory);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            _logger.LogInformation("Processing category deactivation for {CategoryId}.", id);
            var result = await _categoryLifecycle.DeactivateAsync(id);
            switch (result)
            {
                case CategoryDeactivationResult.Deactivated:
                    _logger.LogInformation("Category {CategoryId} was deactivated successfully.", id);
                    break;
                case CategoryDeactivationResult.NotFound:
                    _logger.LogWarning("Category {CategoryId} was not found for deactivation.", id);
                    TempData["Error"] = "No se encontró la categoría que se intentó desactivar.";
                    break;
                case CategoryDeactivationResult.AlreadyInactive:
                    _logger.LogInformation("Category {CategoryId} was inactive when the deactivation result was checked; this request made no state change.", id);
                    break;
                case CategoryDeactivationResult.HasBooks:
                    _logger.LogWarning("Category {CategoryId} could not be deactivated because it is associated with a book.", id);
                    TempData["Error"] = "No se puede desactivar una categoría relacionada con un libro.";
                    break;
                default:
                    _logger.LogWarning("Category {CategoryId} remained active after the deactivation request.", id);
                    TempData["Error"] = "No se pudo desactivar la categoría. Actualiza la página e inténtalo de nuevo.";
                    break;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(Guid id)
        {
            _logger.LogInformation("Processing category restoration for {CategoryId}.", id);
            if (await _categoryLifecycle.RestoreAsync(id))
            {
                _logger.LogInformation("Category {CategoryId} was restored successfully.", id);
            }
            else
            {
                var category = await _categoryQueries.GetByIdWithBooksAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category {CategoryId} was not found for restoration.", id);
                }
                else if (category.IsActive)
                {
                    _logger.LogInformation("Category {CategoryId} was active when the restoration result was checked; this request made no state change.", id);
                }
                else
                {
                    _logger.LogWarning("Category {CategoryId} remained inactive after the restoration request.", id);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
