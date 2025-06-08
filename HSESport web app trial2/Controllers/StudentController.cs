using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSESport_web_app_trial2.Controllers
{
    public class StudentController : Controller
    {
        private readonly ILogger<StudentController> _logger;
        private readonly MyDbContextStudents _context;

        public StudentController(ILogger<StudentController> logger, MyDbContextStudents dbContext)
        {
            _logger = logger;
            _context = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> StudentPersonalAccount(int userId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userId);

            if (student == null)
                return NotFound();

            ViewBag.UserRole = "Student";
            ViewBag.UserId = userId;
            ViewBag.StudentName = student.Name;
            ViewBag.StudentSurname = student.Surname;
            ViewBag.StudentSecondName = student.SecondName;
            ViewBag.StudentEmail = student.Email;
            ViewBag.StudentAttendanceRate = student.AttendanceRate;

            return View(student);
        }
    }
}
