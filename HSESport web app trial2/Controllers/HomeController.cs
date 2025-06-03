using System.Diagnostics;
using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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

        public IActionResult TeacherAuthorization()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TeacherEnter([Bind("Email,Password")] BaseUserModel user)
        {
            if (ModelState.IsValid)
            {
                if (user.Email == "ymgordeev@hse.ru" && user.Password == "12345678")
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

        public IActionResult TeacherMainPage(BaseUserModel teacher)
        {
            return View(teacher);
        }
        public IActionResult TeacherEnterError()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TeacherPersonalInformation([Bind("Email,Password")] BaseUserModel user)
        {
            if (ModelState.IsValid)
            {
                if (user.Email == "ymgordeev@hse.ru" && user.Password == "12345678")
                {
                    user.Name = "Юрий";
                    user.Surname = "Гордеев";
                    user.SecondName = "Матвеевич";
                }
                return RedirectToAction(nameof(TeacherPersonalAccount), "Home", user);
            }
            return View(user);
        }

        public IActionResult TeacherPersonalAccount(BaseUserModel teacher)
        {
            ViewBag.UserRole = "Teacher";
            return View(teacher);
        }

        public IActionResult StudentAuthorization()
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
        public async Task<IActionResult> StudentEnter([Bind("Email,Password")] Students user)
        {
            if (ModelState.IsValid)
            {
                bool doesStudentWithEmailExist = await SearchStudentByEmailAndPassword(user.Email, user.Password);
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
        public async Task<IActionResult> StudentPersonalInformation([Bind("Email,Password")] Students user)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Students.FirstOrDefaultAsync(m => m.Email == user.Email);
                return RedirectToAction(nameof(StudentPersonalAccount), "Home", student);
            }
            return View(user);
        }

        public IActionResult StudentMainPage(Students student)
        {
            return View(student);
        }

        public IActionResult StudentPersonalAccount(Students student)
        {
            return View(student);
        }

        public IActionResult StudentEnterError()
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
