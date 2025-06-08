using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace HSESport_web_app_trial2.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ILogger<TeacherController> _logger;
        private readonly MyDbContextStudents _context_Students;

        private readonly MyDbContextTeachers _context_Teachers;

        public TeacherController(ILogger<TeacherController> logger, MyDbContextTeachers dbContextTeachers, MyDbContextStudents dbContextStudents)
        {
            _logger = logger;
            _context_Teachers = dbContextTeachers;
            _context_Students = dbContextStudents;
        }

        [HttpGet]
        public async Task<IActionResult> TeacherPersonalAccount(int userId)
        {
            var teacher = await _context_Teachers.Teachers
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
        }

        public async Task<IActionResult> SectionStudentsList(int userId)
        {
            var teacher = await _context_Teachers.Teachers.FirstOrDefaultAsync(teacher => teacher.TeacherId == userId);
            if (teacher == null)
            {
                return NotFound();
            }
            else
            {
                var studentAttendanceDates = await _context_Teachers.AttendanceDates
                    .Where(attendance => attendance.SectionId == teacher.SportSectionId)
                    .ToListAsync();
                var pairs = (from attendance in studentAttendanceDates
                            from student in _context_Students.Students
                            where student.StudentId == attendance.StudentId
                            select (AttendanceDate: attendance.Date, StudentName: student.Name)).ToList();
                var section = await _context_Teachers.Sections.FirstOrDefaultAsync(section => section.SectionId == teacher.SportSectionId);
                ViewBag.UserRole = "Teacher";
                ViewBag.SectionName = section?.Name;
                ViewBag.UserId = userId;
                return View(pairs);
            }
        }

        public IActionResult AddingStudentsToSection(int userId)
        {
            ViewBag.UserRole = "Teacher";
            ViewBag.UserId = userId;
            return View();
        }
    }
}
