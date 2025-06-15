using HSESport_web_app_trial2.Controllers;
using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;

namespace HSESport_web_app_trial2.Tests
{
    [TestClass]
    public class StudentControllerTests
    {
        private MyDbContextTeachers _context;
        private StudentController _controller;
        private Mock<ILogger<StudentController>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<MyDbContextTeachers>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;

            _context = new MyDbContextTeachers(options);
            _mockLogger = new Mock<ILogger<StudentController>>();
            _controller = new StudentController(_mockLogger.Object, _context);


            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            // Add a student for testing
            _context.Students.Add(new Students
            {
                StudentId = 1,
                Name = "Анна",
                Surname = "Иванова",
                SecondName = "Сергеевна",
                Email = "anna.ivanova@hse.ru",
                Password = "studentpass",
                AttendanceRate = 10
            });
            _context.Students.Add(new Students
            {
                StudentId = 2,
                Name = "Петр",
                Surname = "Сидоров",
                SecondName = "Алексеевич",
                Email = "petr.sidorov@hse.ru",
                Password = "studentpass2",
                AttendanceRate = 5
            });

            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [TestMethod]
        public async Task StudentPersonalAccount_ReturnsViewWithStudentData_WhenStudentExists()
        {
            // Arrange
            int userId = 1; //

            // Act
            var result = await _controller.StudentPersonalAccount(userId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);

            Assert.IsInstanceOfType(result.Model, typeof(Students));
            var student = result.Model as Students;

            Assert.AreEqual(userId, student.StudentId);
            Assert.AreEqual("Анна", student.Name);
            Assert.AreEqual("Иванова", student.Surname);
            Assert.AreEqual("Сергеевна", student.SecondName);
            Assert.AreEqual("anna.ivanova@hse.ru", student.Email);
            Assert.AreEqual(10, student.AttendanceRate);
            Assert.AreEqual("Student", result.ViewData["UserRole"]);
            Assert.AreEqual(userId, result.ViewData["UserId"]);
            Assert.AreEqual("Анна", result.ViewData["StudentName"]);
            Assert.AreEqual("Иванова", result.ViewData["StudentSurname"]);
            Assert.AreEqual("Сергеевна", result.ViewData["StudentSecondName"]);
            Assert.AreEqual("anna.ivanova@hse.ru", result.ViewData["StudentEmail"]);
            Assert.AreEqual(10, result.ViewData["StudentAttendanceRate"]);
        }

        [TestMethod]
        public async Task StudentPersonalAccount_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            // Arrange
            int userId = 999; 

            // Act
            var result = await _controller.StudentPersonalAccount(userId) as NotFoundResult;

            // Assert

            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode); 
        }
    }
}