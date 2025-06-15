using HSESport_web_app_trial2.Controllers;
using HSESport_web_app_trial2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HSESport_web_app_trial2.Tests
{
    [TestClass]
    public class StudentsDataBaseControllerTests
    {
        private MyDbContextTeachers _context;
        private StudentsDataBaseController _controller;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<MyDbContextTeachers>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MyDbContextTeachers(options);
            _controller = new StudentsDataBaseController(_context);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _context.Students.AddRange(
                new Students { StudentId = 1, Name = "Иван", Surname = "Иванов", Email = "ivan@test.com", Password = "pass1", AttendanceRate = 5, SecondName = "Иванович" },
                new Students { StudentId = 2, Name = "Петр", Surname = "Петров", Email = "petr@test.com", Password = "pass2", AttendanceRate = 10, SecondName = "Петрович" },
                new Students { StudentId = 3, Name = "Анна", Surname = "Сидорова", Email = "anna@test.com", Password = "pass3", AttendanceRate = 3, SecondName = "Андреевна" }
            );
            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [TestMethod]
        public async Task Index_ReturnsViewResultWithStudentsForDataBaseModel()
        {
            // Arrange
            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(StudentsForDataBase));
            var model = result.Model as StudentsForDataBase;

            Assert.IsNotNull(model.StudentsList);
            Assert.AreEqual(3, model.StudentsList.Count);
            Assert.AreEqual("", model.Name);
            Assert.AreEqual("ymgordeev@hse.ru", model.Email);
            Assert.AreEqual("12345678", model.Password);
        }

        [TestMethod]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            int? id = null;

            // Act
            var result = await _controller.Details(id) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Details_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            // Arrange
            int? id = 999;

            // Act
            var result = await _controller.Details(id) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Details_ReturnsViewResultWithStudent_WhenStudentExists()
        {
            // Arrange
            int? id = 1;

            // Act
            var result = await _controller.Details(id) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Students));
            var student = result.Model as Students;
            Assert.AreEqual(id, student.StudentId);
            Assert.AreEqual("Иван", student.Name);
        }

        [TestMethod]
        public void Create_ReturnsViewResult()
        {
            // Arrange
            // Act
            var result = _controller.Create() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Create_RedirectsToIndex_WhenModelStateIsValid()
        {
            // Arrange
            var newStudent = new Students { StudentId = 4, Name = "Ольга", Surname = "Смирнова", Email = "olga@test.com", Password = "newpass", SecondName = "Игоревна" };
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Create(newStudent) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(StudentsDataBaseController.Index), result.ActionName);

            var addedStudent = await _context.Students.FindAsync(4);
            Assert.IsNotNull(addedStudent);
            Assert.AreEqual("Ольга", addedStudent.Name);
            Assert.AreEqual("olga@test.com", addedStudent.Email);
        }

        [TestMethod]
        public async Task Create_ReturnsViewWithModel_WhenModelStateIsInvalid()
        {
            // Arrange
            var invalidStudent = new Students { Name = "Invalid" };
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.Create(invalidStudent) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(invalidStudent, result.Model);
            Assert.IsFalse(_controller.ModelState.IsValid);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            int? id = null;

            // Act
            var result = await _controller.Edit(id) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            // Arrange
            int? id = 999;

            // Act
            var result = await _controller.Edit(id) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Edit_ReturnsViewResultWithStudent_WhenStudentExists()
        {
            // Arrange
            int? id = 1;

            // Act
            var result = await _controller.Edit(id) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Students));
            var student = result.Model as Students;
            Assert.AreEqual(id, student.StudentId);
            Assert.AreEqual("Иван", student.Name);
        }

        [TestMethod]
        public async Task Edit_ReturnsNotFound_WhenIdDoesNotMatchStudentId()
        {
            // Arrange
            int id = 99;
            var studentToUpdate = new Students { StudentId = 1, Name = "Иван", Surname = "Иванов", Email = "ivan@test.com" };
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(id, studentToUpdate) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Edit_ReturnsViewWithModel_WhenModelStateIsInvalid()
        {
            // Arrange
            int id = 1;
            var invalidStudent = new Students { StudentId = 1, Name = "Invalid", Email = "invalid-email" };
            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            var result = await _controller.Edit(id, invalidStudent) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(invalidStudent, result.Model);
            Assert.IsFalse(_controller.ModelState.IsValid);
        }


        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            int? id = null;

            // Act
            var result = await _controller.Delete(id) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Delete_ReturnsNotFound_WhenStudentDoesNotExist()
        {
            // Arrange
            int? id = 999;

            // Act
            var result = await _controller.Delete(id) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Delete_ReturnsViewResultWithStudent_WhenStudentExists()
        {
            // Arrange
            int? id = 1;

            // Act
            var result = await _controller.Delete(id) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Students));
            var student = result.Model as Students;
            Assert.AreEqual(id, student.StudentId);
            Assert.AreEqual("Иван", student.Name);
        }

        [TestMethod]
        public async Task DeleteConfirmed_RemovesStudentAndRedirectsToIndex_WhenStudentExists()
        {
            // Arrange
            int id = 1;
            var initialStudentCount = _context.Students.Count();

            // Act
            var result = await _controller.DeleteConfirmed(id) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(StudentsDataBaseController.Index), result.ActionName);

            var studentInDb = await _context.Students.FindAsync(id);
            Assert.IsNull(studentInDb);
            Assert.AreEqual(initialStudentCount - 1, _context.Students.Count());
        }

        [TestMethod]
        public async Task DeleteConfirmed_RedirectsToIndex_WhenStudentDoesNotExist()
        {
            // Arrange
            int id = 999;
            var initialStudentCount = _context.Students.Count();

            // Act
            var result = await _controller.DeleteConfirmed(id) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(nameof(StudentsDataBaseController.Index), result.ActionName);
            Assert.AreEqual(initialStudentCount, _context.Students.Count());
        }
    }
}