using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult TeacherMainPage(BaseUserModel teacher)
        {
            return View(teacher);
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
    }
}
