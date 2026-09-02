using libraryMVC.Interfaces;
using libraryMVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

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

        public IActionResult Autores()
        {
            return View();
        }

        public IActionResult Categorias()
        {
            return View();
        }

        public IActionResult Usuarios()
        {
            return View();
        }

        public IActionResult Prestamos()
        {
            return View();
        }

        public IActionResult AcercaDe()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
