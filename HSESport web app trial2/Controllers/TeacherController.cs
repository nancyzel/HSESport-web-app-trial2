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

            var section = await _context_Teachers.Sections
                .FirstOrDefaultAsync(s => s.SectionId == teacher.SportSectionId);

            ViewBag.UserRole = "Teacher";
            ViewBag.UserId = userId;
            ViewBag.TeacherName = teacher.Name;
            ViewBag.TeacherSurname = teacher.Surname;
            ViewBag.TeacherSecondName = teacher.SecondName;
            ViewBag.TeacherEmail = teacher.Email;
            ViewBag.SectionName = section?.Name;

            return View(teacher);
        }

        [HttpGet]
        public async Task<IActionResult> SectionStudentsList(int userId)
        {
            var teacher = await _context_Teachers.Teachers
                .FirstOrDefaultAsync(teacher => teacher.TeacherId == userId);
            if (teacher == null)
            {
                return NotFound();
            }

            var section = await _context_Teachers.Sections
                .FirstOrDefaultAsync(section => section.SectionId == teacher.SportSectionId);
            // Загружаем всех студентов
            var students = await _context_Students.Students.ToListAsync();

            ViewBag.UserRole = "Teacher";
            ViewBag.SectionName = section?.Name;
            ViewBag.UserId = userId;
            ViewBag.Students = students;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendance(int userId, int studentId)
        {
            var teacher = await _context_Teachers.Teachers
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null)
                return NotFound();

            var student = await _context_Students.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound();

            // Проверяем, нет ли уже посещения для этого студента в этой секции сегодня
            //var existingAttendance = await _context_Teachers.AttendanceDates
            //    .AnyAsync(a => a.StudentId == studentId && a.SectionId == teacher.SportSectionId && a.Date == DateOnly.FromDateTime(DateTime.Now));
            //if (existingAttendance)
            //{
            //    TempData["Error"] = "Студент уже отмечен как посетивший секцию сегодня.";
            //    return RedirectToAction(nameof(SectionStudentsList), new { userId });
            //}

            // Создаём запись в AttendanceDates
            var attendance = new AttendanceDates
            {
                StudentId = studentId,
                SectionId = teacher.SportSectionId,
                Date = DateOnly.FromDateTime(DateTime.Now)
            };
            _context_Teachers.AttendanceDates.Add(attendance);

            // Увеличиваем AttendanceRate студента
            student.AttendanceRate++;
            _context_Students.Update(student);

            // Сохраняем изменения
            await _context_Teachers.SaveChangesAsync();
            await _context_Students.SaveChangesAsync();

            TempData["Success"] = "Посещение успешно добавлено!";
            return RedirectToAction(nameof(SectionStudentsList), new { userId });
        }

        public IActionResult AddingStudentsToSection(int userId)
        {
            ViewBag.UserRole = "Teacher";
            ViewBag.UserId = userId;
            return View();
        }
    }
}