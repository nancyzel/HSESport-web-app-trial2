using System.Diagnostics;
using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSESport_web_app_trial2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyDbContextTeachers _context_Teachers;
        private readonly MyDbContextTeachers _context_;


        public HomeController(ILogger<HomeController> logger,MyDbContextTeachers dbContextTeachers)
        {
            _logger = logger;
            _context_Teachers = dbContextTeachers;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Help(string userRole, int userId)
        {
            ViewBag.UserRole = userRole;
            ViewBag.UserId = userId;
            return View();
        }

        public IActionResult Schedule(string userRole, int userId)
        {
            ViewBag.UserRole = userRole;
            ViewBag.UserId = userId;
            return View();
        }

        public IActionResult TeacherAuthorization()
        {
            return View();
        }


        public async Task<int> SearchTeacherByEmailAndPassword(string userEmail, string userPassword)
        {
            if (_context_Teachers.Teachers == null)
            {
                return -1;
            }
            else
            {
                var teachers = await _context_Teachers.Teachers.FirstOrDefaultAsync(m => m.Email == userEmail && m.Password == userPassword);
                if (teachers == null)
                {
                    return -1;
                }
                else
                {
                    return teachers.TeacherId;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> TeacherEnter([Bind("Email,Password")] BaseUserModel user)
        {
            if (ModelState.IsValid)
            {
                int teacherId = await SearchTeacherByEmailAndPassword(user.Email, user.Password);
                if (teacherId > 0)
                    return RedirectToAction("SectionStudentsList", "Teacher", new { userId = teacherId });
                else
                {
                    return RedirectToAction(nameof(TeacherEnterError));
                }
            }
            return View(user);
        }

        public IActionResult TeacherEnterError()
        {
            return View();
        }

        public IActionResult StudentAuthorization()
        {
            return View();
        }

        public async Task<int> SearchStudentByEmailAndPassword(string userEmail, string userPassword)
        {
            if (_context_Teachers.Students == null)
            {
                return -1;
            }
            else
            {
                var students = await _context_Teachers.Students.FirstOrDefaultAsync(m => m.Email == userEmail && m.Password == userPassword);
                if (students == null)
                {
                    return -1;
                }
                else
                {
                    return students.StudentId;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> StudentEnter([Bind("Email,Password")] Students user)
        {
            if (ModelState.IsValid)
            {
                int studentId = await SearchStudentByEmailAndPassword(user.Email, user.Password);
                if (studentId > 0)
                    return RedirectToAction("StudentPersonalAccount", "Student", new { userId = studentId });
                else
                {
                    return RedirectToAction(nameof(StudentEnterError));
                }
            }
            return View(user);
        }

        public IActionResult StudentEnterError()
        {
            return View();
        }

        public IActionResult News(string userRole, int userId)
        {
            ViewBag.UserRole = userRole;
            ViewBag.UserId = userId;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
