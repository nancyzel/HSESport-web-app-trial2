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

        public async Task<bool> SearchStudentByEmailAndPassword(string userEmail, string userPassword)
        {
            if (_context.Students == null)
            {
                return false;
            }
            else
            {
                var students = await _context.Students.FirstOrDefaultAsync(m => (m.Email == userEmail && m.Password == userPassword));
                if (students == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> StudentEnter([Bind("UserEmail,UserPassword")] Authorization user)
        {
            if (ModelState.IsValid)
            {
                bool doesStudentWithEmailExist = await SearchStudentByEmailAndPassword(user.UserEmail, user.UserPassword);
                if (doesStudentWithEmailExist)
                    return RedirectToAction(nameof(StudentMainPage), "Home", user);
                else
                {
                    return RedirectToAction(nameof(StudentEnterError));
                }
            }
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> StudentPersonalAccountEnter([Bind("UserEmail,UserPassword")] Authorization user)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Students.FirstOrDefaultAsync(m => m.Email == user.UserEmail);
                return RedirectToAction(nameof(StudentMainPage), "Home", student);
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
                    return RedirectToAction(nameof(TeacherMainPage), "Home", user);
                }
                else
                {
                    return RedirectToAction(nameof(TeacherEnterError));
                }
            }
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> StudentPersonalInformation([Bind("UserEmail,UserPassword")] Authorization user)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Students.FirstOrDefaultAsync(m => m.Email == user.UserEmail);
                Console.WriteLine("ok");
                return View(student);
            }
            Console.WriteLine("not ok");
            return View(user);
        }

        public IActionResult TeacherPersonalInformation()
        {
            return View();
        }

        public IActionResult StudentMainPage(Authorization student)
        {
            return View(student);
        }

        public IActionResult TeacherMainPage(Authorization teacher)
        {
            return View(teacher);
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
