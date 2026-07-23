using Microsoft.AspNetCore.Mvc;

namespace libraryMVC.Controllers
{
    public class BooksController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
