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
        public async Task<IActionResult> SectionStudentsList(int userId, int? sectionId = null, string? searchSurname = null, bool showAllStudentsForAdd = false)
        {
            var teacher = await _context_Teachers.Teachers
                .Include(t => t.TeacherSections)
                    .ThenInclude(ts => ts.Section)
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null)
            {
                return NotFound();
            }

            List<HSESport_web_app_trial2.Models.Sections> teacherSectionsList = teacher.TeacherSections.Select(ts => ts.Section).ToList();
            var teacherSectionIds = teacherSectionsList.Select(s => s.SectionId).ToList();

            string currentSectionName = "Все доступные секции"; // Имя для отображения в заголовке
            string effectiveSectionId = sectionId?.ToString() ?? ""; // ID секции, которая будет использоваться для фильтрации/цели

            // Если sectionId не передан в URL (первая загрузка или сброс фильтра),
            // и у учителя есть хотя бы одна секция, выбираем первую секцию по умолчанию.
            // При этом сохраняем режим отображения.
            if (!sectionId.HasValue && teacherSectionsList.Any())
            {
                var defaultSection = teacherSectionsList.First();
                effectiveSectionId = defaultSection.SectionId.ToString();
                currentSectionName = defaultSection.Name;

                // Важно: перенаправляем, чтобы URL обновился с ID выбранной по умолчанию секции.
                return RedirectToAction("SectionStudentsList", "Teacher", new { userId = userId, sectionId = defaultSection.SectionId, searchSurname = searchSurname, showAllStudentsForAdd = showAllStudentsForAdd });
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
                    // Сбрасываем ID, чтобы не фильтровать по некорректной секции.
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
                ViewBag.ShowAllStudentsForAdd = showAllStudentsForAdd;
                return View(); // Ранний выход, если нет секций
            }

            IQueryable<Students> studentsQuery;

            if (showAllStudentsForAdd)
            {
                // Для режима добавления: показываем ВСЕХ студентов
                studentsQuery = _context_Teachers.Students.AsQueryable();
            }
            else
            {
                // Для обычного режима: показываем студентов, привязанных к секциям учителя
                studentsQuery = _context_Teachers.StudentsSections
                                                .Include(ss => ss.Student)
                                                .Where(ss => teacherSectionIds.Contains(ss.SectionId))
                                                .Select(ss => ss.Student);
            }

            // Применяем фильтр по конкретной секции, если effectiveSectionId не пуст
            // Этот фильтр применяется только в "обычном" режиме, НЕ в режиме показа всех студентов для добавления.
            if (!string.IsNullOrEmpty(effectiveSectionId) && !showAllStudentsForAdd)
            {
                int filterSectionId = int.Parse(effectiveSectionId);
                studentsQuery = studentsQuery.Where(s => s.StudentsSections.Any(ss => ss.SectionId == filterSectionId));
            }

            //  Применяем фильтр по фамилии, если searchSurname не пуст (работает в обоих режимах)
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
            ViewBag.ShowAllStudentsForAdd = showAllStudentsForAdd; // Передаем режим отображения в View

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
                .Include(s => s.StudentsSections)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return NotFound();

            // Проверяем, привязан ли студент к этой секции через StudentsSections
            if (!student.StudentsSections.Any(ss => ss.SectionId == sectionId))
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
                .Include(s => s.StudentsSections)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return NotFound();

            if (student.AttendanceRate <= 0)
            {
                TempData["Error"] = $"Невозможно удалить посещение. У студента {student.Name} {student.Surname} уже 0 посещений.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> AddStudentToSection(int userId, int studentId, int sectionId, string? searchSurname)
        {
            // Проверяем права учителя на секцию
            var teacher = await _context_Teachers.Teachers
                .Include(t => t.TeacherSections)
                    .ThenInclude(ts => ts.Section) // Включаем, чтобы получить имя секции для сообщения
                .FirstOrDefaultAsync(t => t.TeacherId == userId);

            if (teacher == null || !teacher.TeacherSections.Any(ts => ts.SectionId == sectionId))
            {
                TempData["Error"] = "Учитель не имеет права добавлять студентов в эту секцию или учитель не найден.";
                return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId, showAllStudentsForAdd = true, searchSurname });
            }

            // Проверяем существование студента
            var student = await _context_Teachers.Students.FindAsync(studentId);
            if (student == null)
            {
                TempData["Error"] = "Студент не найден.";
                return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId, showAllStudentsForAdd = true, searchSurname });
            }

            // Проверяем, не закреплен ли студент уже за этой секцией
            var existingStudentSection = await _context_Teachers.StudentsSections
                .FirstOrDefaultAsync(ss => ss.StudentId == studentId && ss.SectionId == sectionId);

            if (existingStudentSection != null)
            {
                TempData["Warning"] = $"Студент {student.Name} {student.Surname} уже закреплен за секцией '{teacher.TeacherSections.First(ts => ts.SectionId == sectionId).Section.Name}'.";
                return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId, showAllStudentsForAdd = true, searchSurname });
            }

            // Добавляем новую запись в таблицу связей StudentsSections
            var newStudentSection = new StudentsSections
            {
                StudentId = studentId,
                SectionId = sectionId
            };
            _context_Teachers.StudentsSections.Add(newStudentSection);
            await _context_Teachers.SaveChangesAsync();

            TempData["Success"] = $"Студент {student.Name} {student.Surname} успешно добавлен на секцию '{teacher.TeacherSections.First(ts => ts.SectionId == sectionId).Section.Name}'.";
            // После добавления возвращаемся в обычный режим отображения студентов секции
            return RedirectToAction(nameof(SectionStudentsList), new { userId, sectionId, showAllStudentsForAdd = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTeacherCredentials(int userId, string newEmail, string? newPassword)
        {
            var teacher = await _context_Teachers.Teachers.FindAsync(userId);

            if (teacher == null)
            {
                TempData["Error"] = "Учитель не найден. Невозможно обновить данные.";
                return RedirectToAction(nameof(TeacherPersonalAccount), new { userId = userId });
            }

            // Обновляем Email, если он был изменен и не пуст
            if (!string.IsNullOrEmpty(newEmail) && teacher.Email != newEmail)
            {
                // Проверим, нет ли уже такого email у другого преподавателя
                if (await _context_Teachers.Teachers.AnyAsync(t => t.Email == newEmail && t.TeacherId != userId))
                {
                    TempData["Error"] = "Email уже используется другим преподавателем.";
                    return RedirectToAction(nameof(TeacherPersonalAccount), new { userId = userId });
                }
                teacher.Email = newEmail;
            }

            // Обновляем пароль, если он был введен (не пуст)
            if (!string.IsNullOrEmpty(newPassword))
            {
                // ХЭШширование
                teacher.Password = newPassword;
            }

            try
            {
                _context_Teachers.Teachers.Update(teacher);
                await _context_Teachers.SaveChangesAsync();
                TempData["Success"] = "Данные успешно обновлены!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении данных преподавателя ID: {UserId}", userId);
                TempData["Error"] = "Произошла ошибка при сохранении данных.";
            }

            return RedirectToAction(nameof(TeacherPersonalAccount), new { userId = userId });
        }


        public IActionResult AddingStudentsToSection(int userId)
        {
            ViewBag.UserRole = "Teacher";
            ViewBag.UserId = userId;
            return View();
        }
    }
}
