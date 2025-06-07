using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace HSESport_web_app_trial2.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ILogger<TeacherController> _logger;
        private readonly MyDbContext _context;

        public TeacherController(ILogger<TeacherController> logger, MyDbContext dbContext)
        {
            _logger = logger;
            _context = dbContext;
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
                return RedirectToAction(nameof(TeacherPersonalAccount), "Teacher", user);
            }
            return View(user);
        }

        public IActionResult TeacherPersonalAccount(BaseUserModel teacher)
        {
            ViewBag.UserRole = "Teacher";
            return View(teacher);
        }

        /*[HttpGet]
        public async Task<IActionResult> TeacherPersonalAccount(int userId)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null)
                return NotFound();

            ViewBag.UserRole = "Teacher";
            ViewBag.UserId = userId;
            ViewBag.TeacherName = teacher.Name;
            ViewBag.TeacherSurname = teacher.Surname;
            ViewBag.TeacherSecondName = teacher.SecondName;
            ViewBag.TeacherEmail = teacher.Email;
            ViewBag.TeacherSportSectionId = teacher.SportSectionId;

            return View(teacher);
        }*/
    }
}
