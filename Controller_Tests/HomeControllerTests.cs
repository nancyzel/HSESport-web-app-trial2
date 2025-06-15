using HSESport_web_app_trial2.Controllers;
using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Http; // Required for DefaultHttpContext
using Microsoft.AspNetCore.Routing; // Required for RouteData

namespace HSESport_web_app_trial2.Tests
{
    [TestClass]
    public class HomeControllerTests
    {
        private MyDbContextTeachers _context;
        private HomeController _controller;
        private Mock<ILogger<HomeController>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<MyDbContextTeachers>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MyDbContextTeachers(options);
            _mockLogger = new Mock<ILogger<HomeController>>();

            _controller = new HomeController(_mockLogger.Object, _context);

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _context.Teachers.Add(new Teachers { TeacherId = 1, Name = "Иван", Surname = "Петров", Email = "teacher@test.com", Password = "password123", SecondName = "Иванович" });
            _context.Teachers.Add(new Teachers { TeacherId = 2, Name = "Мария", Surname = "Сидорова", Email = "anotherteacher@test.com", Password = "pass456", SecondName = "Александровна" });

            _context.Students.Add(new Students { StudentId = 1, Name = "Алексей", Surname = "Иванов", Email = "student@test.com", Password = "studentpass", AttendanceRate = 10 });
            _context.Students.Add(new Students { StudentId = 2, Name = "Елена", Surname = "Кузнецова", Email = "anotherstudent@test.com", Password = "studentpass2", AttendanceRate = 5 });

            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
        }


        // Index Method Tests

        [TestMethod]
        public void Index_ReturnsViewResult()
        {
            // Arrange

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }


        // Help Method Tests

        [TestMethod]
        public void Help_ReturnsViewResult_WithCorrectViewData()
        {
            // Arrange
            string userRole = "Teacher";
            int userId = 1;

            // Act
            var result = _controller.Help(userRole, userId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userRole, result.ViewData["UserRole"]);
            Assert.AreEqual(userId, result.ViewData["UserId"]);
        }


        // Schedule Method Tests

        [TestMethod]
        public void Schedule_ReturnsViewResult_WithCorrectViewData()
        {
            // Arrange
            string userRole = "Student";
            int userId = 1;

            // Act
            var result = _controller.Schedule(userRole, userId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userRole, result.ViewData["UserRole"]);
            Assert.AreEqual(userId, result.ViewData["UserId"]);
        }


        // TeacherAuthorization Method Tests

        [TestMethod]
        public void TeacherAuthorization_ReturnsViewResult()
        {
            // Arrange

            // Act
            var result = _controller.TeacherAuthorization() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }


        // SearchTeacherByEmailAndPassword Method Tests

        [TestMethod]
        public async Task SearchTeacherByEmailAndPassword_ReturnsTeacherId_WhenCredentialsAreValid()
        {
            // Arrange
            string email = "teacher@test.com";
            string password = "password123";

            // Act
            int teacherId = await _controller.SearchTeacherByEmailAndPassword(email, password);

            // Assert
            Assert.AreEqual(1, teacherId);
        }

        [TestMethod]
        public async Task SearchTeacherByEmailAndPassword_ReturnsNegativeOne_WhenCredentialsAreInvalid()
        {
            // Arrange
            string email = "teacher@test.com";
            string password = "wrongpassword";

            // Act
            int teacherId = await _controller.SearchTeacherByEmailAndPassword(email, password);

            // Assert
            Assert.AreEqual(-1, teacherId);
        }

        [TestMethod]
        public async Task SearchTeacherByEmailAndPassword_ReturnsNegativeOne_WhenTeacherDoesNotExist()
        {
            // Arrange
            string email = "nonexistent@test.com";
            string password = "anypassword";

            // Act
            int teacherId = await _controller.SearchTeacherByEmailAndPassword(email, password);

            // Assert
            Assert.AreEqual(-1, teacherId);
        }


        // TeacherEnter Method Tests

        [TestMethod]
        public async Task TeacherEnter_RedirectsToSectionStudentsList_WhenModelStateIsValidAndTeacherFound()
        {
            // Arrange
            var user = new BaseUserModel { Email = "teacher@test.com", Password = "password123" };
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.TeacherEnter(user) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SectionStudentsList", result.ActionName);
            Assert.AreEqual("Teacher", result.ControllerName);
            Assert.AreEqual(1, result.RouteValues["userId"]);
        }

        [TestMethod]
        public async Task TeacherEnter_RedirectsToTeacherEnterError_WhenModelStateIsValidAndTeacherNotFound()
        {
            // Arrange
            var user = new BaseUserModel { Email = "invalid@test.com", Password = "invalidpassword" };
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.TeacherEnter(user) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(HomeController.TeacherEnterError), result.ActionName);
        }

        [TestMethod]
        public async Task TeacherEnter_ReturnsViewWithModel_WhenModelStateIsInvalid()
        {
            // Arrange
            var user = new BaseUserModel { Email = "test@test.com" };
            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            var result = await _controller.TeacherEnter(user) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(user, result.Model);
        }


        // TeacherEnterError Method Tests

        [TestMethod]
        public void TeacherEnterError_ReturnsViewResult()
        {
            // Arrange

            // Act
            var result = _controller.TeacherEnterError() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }


        // StudentAuthorization Method Tests

        [TestMethod]
        public void StudentAuthorization_ReturnsViewResult()
        {
            // Arrange

            // Act
            var result = _controller.StudentAuthorization() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }


        // SearchStudentByEmailAndPassword Method Tests

        [TestMethod]
        public async Task SearchStudentByEmailAndPassword_ReturnsStudentId_WhenCredentialsAreValid()
        {
            // Arrange
            string email = "student@test.com";
            string password = "studentpass";

            // Act
            int studentId = await _controller.SearchStudentByEmailAndPassword(email, password);

            // Assert
            Assert.AreEqual(1, studentId);
        }

        [TestMethod]
        public async Task SearchStudentByEmailAndPassword_ReturnsNegativeOne_WhenCredentialsAreInvalid()
        {
            // Arrange
            string email = "student@test.com";
            string password = "wrongpass";

            // Act
            int studentId = await _controller.SearchStudentByEmailAndPassword(email, password);

            // Assert
            Assert.AreEqual(-1, studentId);
        }

        [TestMethod]
        public async Task SearchStudentByEmailAndPassword_ReturnsNegativeOne_WhenStudentDoesNotExist()
        {
            // Arrange
            string email = "nonexistentstudent@test.com";
            string password = "anypassword";

            // Act
            int studentId = await _controller.SearchStudentByEmailAndPassword(email, password);

            // Assert
            Assert.AreEqual(-1, studentId);
        }


        // StudentEnter Method Tests

        [TestMethod]
        public async Task StudentEnter_RedirectsToStudentPersonalAccount_WhenModelStateIsValidAndStudentFound()
        {
            // Arrange
            var user = new Students { Email = "student@test.com", Password = "studentpass" };
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.StudentEnter(user) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("StudentPersonalAccount", result.ActionName);
            Assert.AreEqual("Student", result.ControllerName);
            Assert.AreEqual(1, result.RouteValues["userId"]);
        }

        [TestMethod]
        public async Task StudentEnter_RedirectsToStudentEnterError_WhenModelStateIsValidAndStudentNotFound()
        {
            // Arrange
            var user = new Students { Email = "invalid@test.com", Password = "invalidpassword" };
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.StudentEnter(user) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(HomeController.StudentEnterError), result.ActionName);
        }

        [TestMethod]
        public async Task StudentEnter_ReturnsViewWithModel_WhenModelStateIsInvalid()
        {
            // Arrange
            var user = new Students { Email = "test@test.com" };
            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            var result = await _controller.StudentEnter(user) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(user, result.Model);
        }


        // StudentEnterError Method Tests

        [TestMethod]
        public void StudentEnterError_ReturnsViewResult()
        {
            // Arrange

            // Act
            var result = _controller.StudentEnterError() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }


        // News Method Tests

        [TestMethod]
        public void News_ReturnsViewResult_WithCorrectViewData()
        {
            // Arrange
            string userRole = "Admin";
            int userId = 0;

            // Act
            var result = _controller.News(userRole, userId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userRole, result.ViewData["UserRole"]);
            Assert.AreEqual(userId, result.ViewData["UserId"]);
        }


        // Error Method Tests

        [TestMethod]
        public void Error_ReturnsViewResult_WithErrorViewModel()
        {
            // Arrange
            var activity = new Activity("test").Start();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = _controller.Error() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(ErrorViewModel));
            var errorModel = result.Model as ErrorViewModel;
            Assert.IsFalse(string.IsNullOrEmpty(errorModel.RequestId));

            activity.Stop();
        }
    }
}