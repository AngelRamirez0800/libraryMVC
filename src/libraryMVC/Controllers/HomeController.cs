using System.Diagnostics;
using System.Threading.Tasks;
using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBookQueries _booksService;

        public HomeController(IBookQueries booksService)
        {
            _booksService = booksService;
        }

        public async Task<IActionResult> Index()
        {
            var featuredBooks = await _booksService.GetFeaturedBooksAsync(3);
            return View(featuredBooks);
        }

        [Authorize]
        public IActionResult Autores()
        {
            return View();
        }

        [Authorize]
        public IActionResult Categorias()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Usuarios()
        {
            return View();
        }

        [Authorize]
        public IActionResult Prestamos()
        {
            return View();
        }

        [Authorize]
        public IActionResult AcercaDe()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
