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
        // ИЗМЕНЕНИЕ: Добавлен параметр searchSurname для фильтрации
        public async Task<IActionResult> SectionStudentsList(int userId, int? sectionId = null, string? searchSurname = null)
        {
            var teacher = await _context_Teachers.Teachers
                .Include(t => t.TeacherSections)
                    .ThenInclude(ts => ts.Section)
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null)
            {
                return NotFound();
            }

            // ИЗМЕНЕНИЕ: Получаем список секций учителя напрямую из объекта teacher
            List<HSESport_web_app_trial2.Models.Sections> teacherSectionsList = teacher.TeacherSections.Select(ts => ts.Section).ToList();
            var teacherSectionIds = teacherSectionsList.Select(s => s.SectionId).ToList();

            string currentSectionName = "Все доступные секции"; // Имя для отображения в заголовке
            string effectiveSectionId = sectionId?.ToString() ?? ""; // ID секции, которая будет использоваться для фильтрации

            // ИЗМЕНЕНИЕ: Логика выбора первой секции по умолчанию и перенаправление
            // Если sectionId не передан в URL (первая загрузка или сброс фильтра),
            // и у учителя есть хотя бы одна секция, выбираем первую секцию по умолчанию.
            if (!sectionId.HasValue && teacherSectionsList.Any())
            {
                var defaultSection = teacherSectionsList.First();
                effectiveSectionId = defaultSection.SectionId.ToString();
                currentSectionName = defaultSection.Name;

                // Важно: перенаправляем, чтобы URL обновился с ID выбранной по умолчанию секции.
                // Это гарантирует, что контроллер также получит этот ID для фильтрации студентов
                // в последующих запросах и формы будут передавать правильный ID.
                return RedirectToAction("SectionStudentsList", "Teacher", new { userId = userId, sectionId = defaultSection.SectionId, searchSurname = searchSurname });
            }
            else if (sectionId.HasValue) // Если sectionId передан в URL
            {
                // Проверяем, что переданный sectionId действительно принадлежит учителю
                if (teacherSectionIds.Contains(sectionId.Value))
                {
                    effectiveSectionId = sectionId.Value.ToString();
                    currentSectionName = teacherSectionsList
                                                .FirstOrDefault(s => s.SectionId == sectionId.Value)?
                                                .Name ?? "Выбранная секция"; // Название секции, если найдено
                }
                else // Если sectionId передан, но не принадлежит учителю
                {
                    TempData["Error"] = "Выбранная секция не принадлежит текущему учителю.";
                    // Сбрасываем ID, чтобы не фильтровать по некорректной секции, но продолжаем показывать студентов
                    // из всех секций учителя.
                    effectiveSectionId = "";
                    currentSectionName = "Все доступные секции";
                }
            }
            // ИЗМЕНЕНИЕ: Если у учителя нет секций вообще (после проверки sectionId.HasValue)
            else if (!teacherSectionsList.Any())
            {
                currentSectionName = "Нет закрепленных секций";
                ViewBag.Students = new List<Students>(); // Убедимся, что список студентов пуст
                ViewBag.UserRole = "Teacher";
                ViewBag.SectionName = currentSectionName;
                ViewBag.UserId = userId;
                ViewBag.TeacherSections = teacherSectionsList;
                ViewBag.SearchSurname = searchSurname;
                ViewBag.SelectedSectionId = effectiveSectionId;
                return View(); // Ранний выход, если нет секций
            }


            // ИЗМЕНЕНИЕ: Запрос для получения студентов через StudentsSections
            IQueryable<Students> studentsQuery = _context_Teachers.StudentsSections
                                                .Include(ss => ss.Student) // Загружаем связанные объекты Student
                                                .Where(ss => teacherSectionIds.Contains(ss.SectionId)) // Фильтруем по секциям, которые ведет учитель
                                                .Select(ss => ss.Student); // Выбираем только объекты Student

            // ИЗМЕНЕНИЕ: Применяем фильтр по конкретной секции, если effectiveSectionId не пуст
            if (!string.IsNullOrEmpty(effectiveSectionId))
            {
                int filterSectionId = int.Parse(effectiveSectionId); // Парсим ID для фильтрации
                studentsQuery = studentsQuery.Where(s => s.StudentsSections.Any(ss => ss.SectionId == filterSectionId));
            }

            // ИЗМЕНЕНИЕ: Применяем фильтр по фамилии, если searchSurname не пуст
            if (!string.IsNullOrEmpty(searchSurname))
            {
                studentsQuery = studentsQuery.Where(s => s.Surname.Contains(searchSurname));
            }

            var students = await studentsQuery.Distinct().ToListAsync();

            ViewBag.UserRole = "Teacher";
            ViewBag.SectionName = currentSectionName;
            ViewBag.UserId = userId;
            ViewBag.Students = students;
            ViewBag.TeacherSections = teacherSectionsList; // Передаем список секций преподавателя
            ViewBag.SearchSurname = searchSurname; // Передаем фамилию для поиска обратно в View
            ViewBag.SelectedSectionId = effectiveSectionId; // Передаем актуальный selectedSectionId в View (для Razor)

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
