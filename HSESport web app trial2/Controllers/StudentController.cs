using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Убедитесь, что этот using есть
using System; // Для Exception
using Microsoft.Extensions.Logging; // Для ILogger
using System.Collections.Generic; // Для List

namespace HSESport_web_app_trial2.Controllers
{
    public class StudentController : Controller
    {
        private readonly ILogger<StudentController> _logger;
        private readonly MyDbContextTeachers _context; // Используем тот же контекст для всех таблиц

        public StudentController(ILogger<StudentController> logger, MyDbContextTeachers dbContext)
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

            var sectionAttendanceDetails = await _context.AttendanceDates
                .Where(ad => ad.StudentId == userId) 
                .GroupBy(ad => ad.SectionId) 
                .Select(g => new 
                {
                    SectionId = g.Key,
                    AttendanceCount = g.Count()
                })
                .Join(_context.Sections, 
                      sectionGroup => sectionGroup.SectionId, 
                      section => section.SectionId,        
                      (sectionGroup, section) => new          
                      {
                          SectionName = section.Name,         
                          AttendanceCount = sectionGroup.AttendanceCount
                      })
                .OrderBy(s => s.SectionName) 
                .ToListAsync();

            ViewBag.AttendedSectionDetails = sectionAttendanceDetails;

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStudentCredentials(int userId, string newEmail, string? newPassword)
        {
            var student = await _context.Students.FindAsync(userId);

            if (student == null)
            {
                TempData["Error"] = "Студент не найден. Невозможно обновить данные.";
                return RedirectToAction(nameof(StudentPersonalAccount), new { userId = userId });
            }

            if (!string.IsNullOrEmpty(newEmail) && student.Email != newEmail)
            {
                if (await _context.Students.AnyAsync(s => s.Email == newEmail && s.StudentId != userId))
                {
                    TempData["Error"] = "Email уже используется другим студентом.";
                    return RedirectToAction(nameof(StudentPersonalAccount), new { userId = userId });
                }
                student.Email = newEmail;
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                student.Password = newPassword;
            }

            try
            {
                _context.Students.Update(student);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Данные успешно обновлены!";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Ошибка параллельного доступа при обновлении. Попробуйте еще раз.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении данных студента ID: {UserId}", userId);
                TempData["Error"] = "Произошла ошибка при сохранении данных.";
            }

            return RedirectToAction(nameof(StudentPersonalAccount), new { userId = userId });
        }
    }
}
