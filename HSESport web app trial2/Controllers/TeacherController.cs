using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace HSESport_web_app_trial2.Controllers
{
    public class TeacherController : Controller
    {
        private readonly ILogger<TeacherController> _logger;
        private readonly MyDbContextTeachers _context_Teachers; // Теперь этот контекст управляет всеми таблицами

        public TeacherController(ILogger<TeacherController> logger, MyDbContextTeachers dbContextTeachers)
        {
            _logger = logger;
            _context_Teachers = dbContextTeachers;
        }

        [HttpGet]
        public async Task<IActionResult> TeacherPersonalAccount(int userId)
        {
            var teacher = await _context_Teachers.Teachers
                .Include(t => t.TeacherSections) // Загружаем связи с секциями
                    .ThenInclude(ts => ts.Section) // Загружаем сами объекты секций
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null)
                return NotFound();

            ViewBag.UserRole = "Teacher";
            ViewBag.UserId = userId;
            ViewBag.TeacherName = teacher.Name;
            ViewBag.TeacherSurname = teacher.Surname;
            ViewBag.TeacherSecondName = teacher.SecondName;
            ViewBag.TeacherEmail = teacher.Email;

            ViewBag.SectionNames = teacher.TeacherSections.Select(ts => ts.Section.Name).ToList();

            return View(teacher);
        }

        [HttpGet]
        public async Task<IActionResult> SectionStudentsList(int userId, int? sectionId = null)
        {
            var teacher = await _context_Teachers.Teachers
                .Include(t => t.TeacherSections)
                    .ThenInclude(ts => ts.Section)
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null)
            {
                return NotFound();
            }

            var teacherSectionIds = teacher.TeacherSections.Select(ts => ts.SectionId).ToList();

            // Изменяем запрос для получения студентов через StudentsSections
            IQueryable<Students> studentsQuery = _context_Teachers.StudentsSections
                                                .Include(ss => ss.Student) // Загружаем связанные объекты Student
                                                .Where(ss => teacherSectionIds.Contains(ss.SectionId)) // Фильтруем по секциям, которые ведет учитель
                                                .Select(ss => ss.Student); // Выбираем только объекты Student

            string currentSectionName = "Все доступные секции";

            if (sectionId.HasValue && teacherSectionIds.Contains(sectionId.Value))
            {
                // Если выбрана конкретная секция, дополнительно фильтруем студентов по этой секции
                // Student.StudentsSections.Any() используется для проверки, что студент привязан к этой секции
                studentsQuery = studentsQuery.Where(s => s.StudentsSections.Any(ss => ss.SectionId == sectionId.Value)); // <--- ИМЯ НАВИГАЦИОННОГО СВОЙСТВА ИЗМЕНЕНО
                currentSectionName = teacher.TeacherSections
                                            .FirstOrDefault(ts => ts.SectionId == sectionId.Value)?
                                            .Section?.Name ?? "Выбранная секция";
            }

            var students = await studentsQuery.Distinct().ToListAsync(); // .Distinct() чтобы избежать дубликатов

            ViewBag.UserRole = "Teacher";
            ViewBag.SectionName = currentSectionName;
            ViewBag.UserId = userId;
            ViewBag.Students = students;
            ViewBag.TeacherSections = teacher.TeacherSections.Select(ts => ts.Section).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendance(int userId, int studentId, int sectionId)
        {
            var teacher = await _context_Teachers.Teachers
                .Include(t => t.TeacherSections)
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null) return NotFound();
            
            var student = await _context_Teachers.Students
                .Include(s => s.StudentsSections) // <--- ИМЯ НАВИГАЦИОННОГО СВОЙСТВА ИЗМЕНЕНО
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return NotFound();

            // Проверяем, привязан ли студент к этой секции через StudentsSections
            if (!student.StudentsSections.Any(ss => ss.SectionId == sectionId)) // <--- ИМЯ НАВИГАЦИОННОГО СВОЙСТВА ИЗМЕНЕНО
            {
                TempData["Error"] = $"Студент {student.Name} {student.Surname} не привязан к секции {sectionId}, для которой производится запись посещения. Посещение не добавлено.";
                return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId });
            }

            var attendance = new AttendanceDates
            {
                StudentId = studentId,
                SectionId = sectionId,
                Date = DateOnly.FromDateTime(DateTime.Now)
            };
            _context_Teachers.AttendanceDates.Add(attendance);

            student.AttendanceRate++;
            _context_Teachers.Students.Update(student);

            await _context_Teachers.SaveChangesAsync();

            TempData["Success"] = $"Студенту {student.Name} {student.Surname} добавлено посещение: {student.AttendanceRate - 1} + 1 = {student.AttendanceRate}";
            return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendance(int userId, int studentId, int sectionId)
        {
            var teacher = await _context_Teachers.Teachers
                .Include(t => t.TeacherSections)
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null) return NotFound();

            if (!teacher.TeacherSections.Any(ts => ts.SectionId == sectionId))
            {
                TempData["Error"] = "Учитель не имеет права удалять посещения для этой секции.";
                return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId });
            }

            var student = await _context_Teachers.Students
                .Include(s => s.StudentsSections) // <--- ИМЯ НАВИГАЦИОННОГО СВОЙСТВА ИЗМЕНЕНО
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return NotFound();

            if (student.AttendanceRate <= 0)
            {
                TempData["Error"] = $"Невозможно удалить посещение. У студента {student.Name} {student.Surname} уже 0 посещений.";
                return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId });
            }

            // Проверяем, привязан ли студент к этой секции через StudentsSections
            if (!student.StudentsSections.Any(ss => ss.SectionId == sectionId)) // <--- ИМЯ НАВИГАЦИОННОГО СВОЙСТВА ИЗМЕНЕНО
            {
                TempData["Error"] = $"Студент {student.Name} {student.Surname} не привязан к секции {sectionId}. Посещение не удалено.";
                return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId });
            }

            var existingAttendance = await _context_Teachers.AttendanceDates
                .Where(ad => ad.StudentId == studentId && ad.SectionId == sectionId && ad.Date == DateOnly.FromDateTime(DateTime.Now))
                .OrderByDescending(ad => ad.Date)
                .FirstOrDefaultAsync();

            if (existingAttendance != null)
            {
                _context_Teachers.AttendanceDates.Remove(existingAttendance);
            }
            else
            {
                TempData["Error"] = $"Запись о посещении для секции {sectionId} и студента {student.Name} {student.Surname} за сегодня не найдена. Количество посещений будет уменьшено.";
            }

            student.AttendanceRate--;
            _context_Teachers.Students.Update(student);

            await _context_Teachers.SaveChangesAsync();

            TempData["Success"] = $"Студенту {student.Name} {student.Surname} УДАЛЕНО посещение: {student.AttendanceRate + 1} - 1 = {student.AttendanceRate}";
            return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId });
        }

        public IActionResult AddingStudentsToSection(int userId)
        {
            ViewBag.UserRole = "Teacher";
            ViewBag.UserId = userId;
            return View();
        }
    }
}
