using System.Diagnostics;
using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSESport_web_app_trial2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyDbContext _context;

        public HomeController(ILogger<HomeController> logger, MyDbContext dbContext)
        {
            _logger = logger;
            _context = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Schedule()
        {
            return View();
        }

        public IActionResult StudentAuthorization()
        {
            return View();
        }

        public IActionResult TeacherAuthorization()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enter([Bind("UserEmail,UserPassword")] Authorization user)
        {
            Console.WriteLine("дошел до enter");
            if (ModelState.IsValid)
            {
                if (user.UserEmail == "aazelenina@hse.ru" && user.UserPassword == "123456")
                    return RedirectToAction(nameof(Index));
                else
                {
                    return RedirectToAction(nameof(EnterError));
                }
            }
            return View(user);
        }

        public IActionResult EnterError()
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
