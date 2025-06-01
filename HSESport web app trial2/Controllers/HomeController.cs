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

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult StudentEnter([Bind("UserEmail,UserPassword")] Authorization user)
        {
            if (ModelState.IsValid)
            {
                StudentsController newCont = new StudentsController(_context);
                if (newCont.Search(user.UserEmail))
                    return RedirectToAction(nameof(StudentMainPage), "Home", user);
                else
                {
                    return RedirectToAction(nameof(StudentEnterError));
                }
            }
            return View(user);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult TeacherEnter([Bind("UserEmail,UserPassword")] Authorization user)
        {
            if (ModelState.IsValid)
            {
                if (user.UserEmail == "ymgordeev@hse.ru" && user.UserPassword == "12345678")
                {
                    return RedirectToAction(nameof(TeacherMainPage));
                }
                else
                {
                    return RedirectToAction(nameof(TeacherEnterError));
                }
            }
            return View(user);
        }

        public IActionResult StudentMainPage(Authorization student)
        {
            Console.WriteLine(student.UserEmail);
            return View(student);
        }

        public IActionResult TeacherMainPage()
        {
            return View();
        }

        public IActionResult StudentEnterError()
        {
            return View();
        }

        public IActionResult TeacherEnterError()
        {
            return View();
        }

        public IActionResult News()
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
