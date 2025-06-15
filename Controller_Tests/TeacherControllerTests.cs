using HSESport_web_app_trial2.Controllers;
using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Для ITempDataDictionary
using Microsoft.AspNetCore.Http; // Для DefaultHttpContext
using Microsoft.AspNetCore.Routing; // Для RouteData

namespace HSESport_web_app_trial2.Tests
{
    [TestClass]
    public class TeacherControllerTests
    {
        private MyDbContextTeachers _context;
        private TeacherController _controller;
        private Mock<ILogger<TeacherController>> _mockLogger;
        private Mock<ITempDataDictionary> _mockTempData; // Мок для TempData

        // Моки DbContext и DbSet для сценариев, где нужно имитировать поведение базы данных,
        // включая ошибки (вместо MyDbContextTeachers _context;)
        private Mock<MyDbContextTeachers> _mockContext;


        [TestInitialize]
        public void Setup()
        {
            // Настройка in-memory базы данных для большинства тестов
            var options = new DbContextOptionsBuilder<MyDbContextTeachers>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MyDbContextTeachers(options); // Этот экземпляр будет использоваться для большинства тестов

            _mockLogger = new Mock<ILogger<TeacherController>>();
            _mockTempData = new Mock<ITempDataDictionary>();

            _controller = new TeacherController(_mockLogger.Object, _context);

            // Настройка ControllerContext для поддержки TempData
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData() // Добавляем RouteData
            };
            _controller.TempData = _mockTempData.Object;

            // Очищаем и заполняем базу данных перед каждым тестом
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var teacher1 = new Teachers { TeacherId = 1, Name = "Иван", Surname = "Петров", Email = "ivan.petrov@hse.ru", Password = "pass", SecondName = "Иванович" };
            var teacher2 = new Teachers { TeacherId = 2, Name = "Мария", Surname = "Сидорова", Email = "maria.sidorova@hse.ru", Password = "pass", SecondName = "Александровна" };
            _context.Teachers.AddRange(teacher1, teacher2);

            var section1 = new Sections { SectionId = 101, Name = "Футбол" };
            var section2 = new Sections { SectionId = 102, Name = "Баскетбол" };
            var section3 = new Sections { SectionId = 103, Name = "Волейбол" };
            _context.Sections.AddRange(section1, section2, section3);

            _context.TeacherSections.AddRange(
                new TeacherSection { TeacherId = 1, SectionId = 101, Teacher = teacher1, Section = section1 },
                new TeacherSection { TeacherId = 1, SectionId = 102, Teacher = teacher1, Section = section2 }
            );

            var student1 = new Students { StudentId = 1001, Name = "Алексей", Surname = "Иванов", AttendanceRate = 5 };
            var student2 = new Students { StudentId = 1002, Name = "Елена", Surname = "Смирнова", AttendanceRate = 3 };
            var student3 = new Students { StudentId = 1003, Name = "Дмитрий", Surname = "Кузнецов", AttendanceRate = 0 };
            _context.Students.AddRange(student1, student2, student3);

            _context.StudentsSections.AddRange(
                new StudentsSections { StudentId = 1001, SectionId = 101, Student = student1, Section = section1 },
                new StudentsSections { StudentId = 1002, SectionId = 101, Student = student2, Section = section1 },
                new StudentsSections { StudentId = 1003, SectionId = 102, Student = student3, Section = section2 }
            );

            _context.AttendanceDates.AddRange(
                new AttendanceDates { AttendanceId = 1, StudentId = 1001, SectionId = 101, Date = DateOnly.FromDateTime(DateTime.Now) },
                new AttendanceDates { AttendanceId = 2, StudentId = 1001, SectionId = 101, Date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)) }
            );

            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
            
            _mockContext = null;
        }

        // --- Тесты для TeacherPersonalAccount ---
        [TestMethod]
        public async Task TeacherPersonalAccount_ReturnsViewWithTeacherData_WhenTeacherExists()
        {
            // Arrange
            int userId = 1;

            // Act
            var result = await _controller.TeacherPersonalAccount(userId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Teachers));
            var teacher = result.Model as Teachers;
            Assert.AreEqual(userId, teacher.TeacherId);
            Assert.AreEqual("Иван", teacher.Name);
            Assert.AreEqual("Петров", teacher.Surname);
            // Исправлено: доступ к ViewBag через ViewData
            Assert.AreEqual("Teacher", result.ViewData["UserRole"]);
            Assert.AreEqual(userId, result.ViewData["UserId"]);
            // Для SectionNames, который является List<string>
            CollectionAssert.AreEquivalent(new List<string> { "Футбол", "Баскетбол" }, (List<string>)result.ViewData["SectionNames"]);
        }

        [TestMethod]
        public async Task TeacherPersonalAccount_ReturnsNotFound_WhenTeacherDoesNotExist()
        {
            // Arrange
            int userId = 999;

            // Act
            var result = await _controller.TeacherPersonalAccount(userId) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        // --- Тесты для SectionStudentsList ---

        [TestMethod]
        public async Task SectionStudentsList_ReturnsViewWithAllStudentsForTeacher_WhenNoSectionIdProvided()
        {
            // Arrange
            int userId = 1;

            // Act
            var result = await _controller.SectionStudentsList(userId) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            Assert.AreEqual("Teacher", result.ControllerName);
            Assert.AreEqual(userId, result.RouteValues["userId"]);
            Assert.AreEqual(101, result.RouteValues["sectionId"]);
        }

        [TestMethod]
        public async Task SectionStudentsList_ReturnsViewWithFilteredStudents_WhenSectionIdProvided()
        {
            // Arrange
            int userId = 1;
            int sectionId = 101;

            // Act
            var result = await _controller.SectionStudentsList(userId, sectionId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            // Исправлено: доступ к ViewBag через ViewData
            var students = result.ViewData["Students"] as List<Students>;
            Assert.IsNotNull(students);
            Assert.AreEqual(2, students.Count);
            Assert.IsTrue(students.Any(s => s.StudentId == 1001 && s.Surname == "Иванов"));
            Assert.IsTrue(students.Any(s => s.StudentId == 1002 && s.Surname == "Смирнова"));
            Assert.AreEqual("Футбол", result.ViewData["SectionName"]);
        }

        [TestMethod]
        public async Task SectionStudentsList_ReturnsViewWithStudentsFilteredBySurname()
        {
            // Arrange
            int userId = 1;
            int sectionId = 101;
            string searchSurname = "Иванов";

            // Act
            var result = await _controller.SectionStudentsList(userId, sectionId, searchSurname) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            // Исправлено: доступ к ViewBag через ViewData
            var students = result.ViewData["Students"] as List<Students>;
            Assert.IsNotNull(students);
            Assert.AreEqual(1, students.Count);
            Assert.AreEqual(1001, students.First().StudentId);
            Assert.AreEqual("Иванов", students.First().Surname);
        }


        [TestMethod]
        public async Task SectionStudentsList_ReturnsNotFound_WhenTeacherDoesNotExist()
        {
            // Arrange
            int userId = 999;

            // Act
            var result = await _controller.SectionStudentsList(userId) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SectionStudentsList_ReturnsEmptyStudentsList_WhenTeacherHasNoSections()
        {
            // Arrange
            int userId = 2;
            var teacher2Sections = _context.TeacherSections.Where(ts => ts.TeacherId == userId).ToList();
            _context.TeacherSections.RemoveRange(teacher2Sections);
            _context.SaveChanges();

            // Act
            var result = await _controller.SectionStudentsList(userId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            // Исправлено: доступ к ViewBag через ViewData
            var students = result.ViewData["Students"] as List<Students>;
            Assert.IsNotNull(students);
            Assert.AreEqual(0, students.Count);
            Assert.AreEqual("Нет закрепленных секций", result.ViewData["SectionName"]);
        }

        [TestMethod]
        public async Task SectionStudentsList_SetsTempDataError_WhenInvalidSectionIdProvided()
        {
            // Arrange
            int userId = 1;
            int sectionId = 999;

            // Act
            var result = await _controller.SectionStudentsList(userId, sectionId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Error"] = It.Is<string>(s => s.Contains("Выбранная секция не принадлежит")), Times.Once);
            // Исправлено: доступ к ViewBag через ViewData
            Assert.AreEqual("Все доступные секции", result.ViewData["SectionName"]);
            Assert.AreEqual("", result.ViewData["SelectedSectionId"]);
        }

        // --- Тесты для AddAttendance ---

        [TestMethod]
        public async Task AddAttendance_IncreasesAttendanceRateAndAddsRecord_WhenStudentAndSectionValid()
        {
            // Arrange
            int userId = 1;
            int studentId = 1001;
            int sectionId = 101;
            int initialAttendanceRate = _context.Students.Find(studentId).AttendanceRate;

            // Act
            var result = await _controller.AddAttendance(userId, studentId, sectionId) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            //Assert.AreEqual("Teacher", result.ControllerName);
            Assert.AreEqual(userId, result.RouteValues["userId"]);
            Assert.AreEqual(sectionId, result.RouteValues["sectionId"]);

            var updatedStudent = await _context.Students.FindAsync(studentId);
            Assert.AreEqual(initialAttendanceRate + 1, updatedStudent.AttendanceRate);

            var attendanceRecord = await _context.AttendanceDates
                .FirstOrDefaultAsync(ad => ad.StudentId == studentId && ad.SectionId == sectionId && ad.Date == DateOnly.FromDateTime(DateTime.Now));
            Assert.IsNotNull(attendanceRecord);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Success"] = It.Is<string>(s => s.Contains("добавлено посещение")), Times.Once);
        }

        [TestMethod]
        public async Task AddAttendance_ReturnsNotFound_WhenTeacherDoesNotExist()
        {
            // Arrange
            int userId = 999;
            int studentId = 1001;
            int sectionId = 101;

            // Act
            var result = await _controller.AddAttendance(userId, studentId, sectionId) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task AddAttendance_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            // Arrange
            int userId = 1;
            int studentId = 9999;
            int sectionId = 101;

            // Act
            var result = await _controller.AddAttendance(userId, studentId, sectionId) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task AddAttendance_ReturnsRedirectWithTempDataError_WhenStudentNotBoundToSection()
        {
            // Arrange
            int userId = 1;
            int studentId = 1001;
            int sectionId = 103;
            int initialAttendanceRate = _context.Students.Find(studentId).AttendanceRate;

            // Act
            var result = await _controller.AddAttendance(userId, studentId, sectionId) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Error"] = It.Is<string>(s => s.Contains("не привязан к секции")), Times.Once);

            var student = await _context.Students.FindAsync(studentId);
            Assert.AreEqual(initialAttendanceRate, student.AttendanceRate);
        }


        // --- Тесты для DeleteAttendance ---


        [TestMethod]
        public async Task DeleteAttendance_ReturnsNotFound_WhenTeacherDoesNotExist()
        {
            // Arrange
            int userId = 999;
            int studentId = 1001;
            int sectionId = 101;

            // Act
            var result = await _controller.DeleteAttendance(userId, studentId, sectionId) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task DeleteAttendance_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            // Arrange
            int userId = 1;
            int studentId = 9999;
            int sectionId = 101;

            // Act
            var result = await _controller.DeleteAttendance(userId, studentId, sectionId) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task DeleteAttendance_ReturnsRedirectWithTempDataError_WhenAttendanceRateIsZero()
        {
            // Arrange
            int userId = 1;
            int studentId = 1003;
            int sectionId = 102;

            // Act
            var result = await _controller.DeleteAttendance(userId, studentId, sectionId) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Error"] = It.Is<string>(s => s.Contains("уже 0 посещений")), Times.Once);

            var student = await _context.Students.FindAsync(studentId);
            Assert.AreEqual(0, student.AttendanceRate);
        }

        [TestMethod]
        public async Task DeleteAttendance_RemovesRecordAndDecreasesAttendanceRate()
        {
            // Arrange
            int userId = 1; // Учитель, который выполняет действие
            int studentId = 1001; // Студент, чье посещение удаляем
            int sectionId = 101;  // Секция, из которой удаляем посещение

            // Получаем текущий AttendanceRate студента до выполнения действия
            var initialAttendanceRate = _context.Students.Find(studentId).AttendanceRate;

            // Убеждаемся, что запись о посещении за сегодня существует
            var initialAttendanceRecord = await _context.AttendanceDates
                .FirstOrDefaultAsync(ad => ad.StudentId == studentId && ad.SectionId == sectionId && ad.Date == DateOnly.FromDateTime(DateTime.Now));
            Assert.IsNotNull(initialAttendanceRecord, "Должна существовать запись о посещении для удаления.");

            // Act
            // Вызываем метод контроллера для удаления посещения
            var result = await _controller.DeleteAttendance(userId, studentId, sectionId) as RedirectToActionResult;

            // Assert
            // 1. Проверяем, что результат действия - это редирект
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            //Assert.AreEqual("Teacher", result.ControllerName);
            Assert.AreEqual(userId, result.RouteValues["userId"]);
            Assert.AreEqual(sectionId, result.RouteValues["sectionId"]);

            // 2. Проверяем, что счётчик посещаемости студента уменьшился на 1
            var updatedStudent = await _context.Students.FindAsync(studentId);
            Assert.AreEqual(initialAttendanceRate - 1, updatedStudent.AttendanceRate, "AttendanceRate студента должен был уменьшиться на 1.");

            // 3. Проверяем, что запись о посещении за сегодня была удалена
            var attendanceRecordAfterDeletion = await _context.AttendanceDates
                .FirstOrDefaultAsync(ad => ad.StudentId == studentId && ad.SectionId == sectionId && ad.Date == DateOnly.FromDateTime(DateTime.Now));
            Assert.IsNull(attendanceRecordAfterDeletion, "Запись о посещении за сегодня должна быть удалена.");

            // 4. Проверяем, что в TempData было установлено сообщение об успехе
            _mockTempData.VerifySet(
                td => td["Success"] = It.Is<string>(s => s.Contains("УДАЛЕНО посещение")),
                Times.Once,
                "TempData['Success'] должно содержать сообщение об успешном удалении."
            );
        }


            // --- Тесты для AddStudentToSection ---

            [TestMethod]
        public async Task AddStudentToSection_AddsStudentToSectionSuccessfully()
        {
            // Arrange
            int userId = 1;
            int studentId = 1003;
            int sectionId = 101;

            // Act
            var result = await _controller.AddStudentToSection(userId, studentId, sectionId, null) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            Assert.AreEqual(false, result.RouteValues["showAllStudentsForAdd"]);

            var studentSection = await _context.StudentsSections
                .FirstOrDefaultAsync(ss => ss.StudentId == studentId && ss.SectionId == sectionId);
            Assert.IsNotNull(studentSection);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Success"] = It.Is<string>(s => s.Contains("успешно добавлен на секцию")), Times.Once);
        }

        [TestMethod]
        public async Task AddStudentToSection_ReturnsError_WhenTeacherHasNoRightsToSection()
        {
            // Arrange
            int userId = 2;
            int studentId = 1001;
            int sectionId = 101;

            // Act
            var result = await _controller.AddStudentToSection(userId, studentId, sectionId, null) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            Assert.AreEqual(true, result.RouteValues["showAllStudentsForAdd"]);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Error"] = It.Is<string>(s => s.Contains("Учитель не имеет права")), Times.Once);
        }

        [TestMethod]
        public async Task AddStudentToSection_ReturnsError_WhenStudentDoesNotExist()
        {
            // Arrange
            int userId = 1;
            int studentId = 9999;
            int sectionId = 101;

            // Act
            var result = await _controller.AddStudentToSection(userId, studentId, sectionId, null) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            Assert.AreEqual(true, result.RouteValues["showAllStudentsForAdd"]);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Error"] = It.Is<string>(s => s.Contains("Студент не найден.")), Times.Once);
        }

        [TestMethod]
        public async Task AddStudentToSection_ReturnsWarning_WhenStudentAlreadyInSection()
        {
            // Arrange
            int userId = 1;
            int studentId = 1001;
            int sectionId = 101;

            // Act
            var result = await _controller.AddStudentToSection(userId, studentId, sectionId, null) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            Assert.AreEqual(true, result.RouteValues["showAllStudentsForAdd"]);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Warning"] = It.Is<string>(s => s.Contains("уже закреплен за секцией")), Times.Once);
        }

        // --- Тесты для UpdateTeacherCredentials ---

        [TestMethod]
        public async Task UpdateTeacherCredentials_UpdatesEmailAndPasswordField_WhenValidDataProvided()
        {
            // Arrange
            int userId = 1;
            string newEmail = "new.email@hse.ru";
            string newPassword = "newPass";
            var initialTeacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.TeacherId == userId);
            Assert.IsNotNull(initialTeacher);

            // Act
            var result = await _controller.UpdateTeacherCredentials(userId, newEmail, newPassword) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TeacherPersonalAccount", result.ActionName);
            Assert.AreEqual(userId, result.RouteValues["userId"]);

            var updatedTeacher = await _context.Teachers.FindAsync(userId);
            Assert.AreEqual(newEmail, updatedTeacher.Email);
            Assert.AreEqual(newPassword, updatedTeacher.Password);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Success"] = It.Is<string>(s => s.Contains("Данные успешно обновлены!")), Times.Once);
        }

        [TestMethod]
        public async Task UpdateTeacherCredentials_UpdatesEmailOnly_WhenNewPasswordIsEmpty()
        {
            // Arrange
            int userId = 1;
            string newEmail = "only.email@hse.ru";
            string newPassword = "";
            var initialTeacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.TeacherId == userId);
            string originalPassword = initialTeacher.Password;

            // Act
            var result = await _controller.UpdateTeacherCredentials(userId, newEmail, newPassword) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            var updatedTeacher = await _context.Teachers.FindAsync(userId);
            Assert.AreEqual(newEmail, updatedTeacher.Email);
            Assert.AreEqual(originalPassword, updatedTeacher.Password);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Success"] = It.Is<string>(s => s.Contains("Данные успешно обновлены!")), Times.Once);
        }

        [TestMethod]
        public async Task UpdateTeacherCredentials_ReturnsError_WhenTeacherNotFound()
        {
            // Arrange
            int userId = 999;
            string newEmail = "nonexistent@hse.ru";
            string newPassword = "pass";

            // Act
            var result = await _controller.UpdateTeacherCredentials(userId, newEmail, newPassword) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TeacherPersonalAccount", result.ActionName);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Error"] = It.Is<string>(s => s.Contains("Учитель не найден.")), Times.Once);
        }

        [TestMethod]
        public async Task UpdateTeacherCredentials_ReturnsError_WhenEmailAlreadyUsedByAnotherTeacher()
        {
            // Arrange
            int userId = 1;
            string newEmail = "maria.sidorova@hse.ru";
            string newPassword = "pass";

            // Act
            var result = await _controller.UpdateTeacherCredentials(userId, newEmail, newPassword) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TeacherPersonalAccount", result.ActionName);
            // Исправлено: Проверка через Verify сеттера индексатора
            _mockTempData.VerifySet(td => td["Error"] = It.Is<string>(s => s.Contains("Email уже используется другим преподавателем.")), Times.Once);
        }


        // --- Тесты для AddingStudentsToSection ---

        [TestMethod]
        public void AddingStudentsToSection_ReturnsViewWithCorrectViewBagData()
        {
            // Arrange
            int userId = 1;

            // Act
            var result = _controller.AddingStudentsToSection(userId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            // Исправлено: доступ к ViewBag через ViewData
            Assert.AreEqual("Teacher", result.ViewData["UserRole"]);
            Assert.AreEqual(userId, result.ViewData["UserId"]);
        }
    }
}