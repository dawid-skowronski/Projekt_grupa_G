using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SimpleApiBackend.Tests
{
    /// <summary>
    /// Klasa testowa dla kontrolera ExpensesController, testuj¹ca funkcjonalnoœci zwi¹zane z wydatkami i d³ugami.
    /// </summary>
    public class ExpensesControllerAdditionalTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ExpensesController _controller;

        /// <summary>
        /// Konstruktor inicjalizuj¹cy bazê danych w pamiêci oraz instancjê kontrolera.
        /// </summary>
        public ExpensesControllerAdditionalTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _controller = new ExpensesController(_dbContext);
        }

        /// <summary>
        /// Ustawia kontekst u¿ytkownika dla testowanego ¿¹dania.
        /// </summary>
        /// <param name="userId">ID u¿ytkownika do ustawienia w tokenie.</param>
        private void SetUserContext(int userId)
        {
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(userClaims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = userPrincipal
                }
            };
        }

        /// <summary>
        /// Sprawdza, czy metoda CreateExpense zwraca BadRequest, jeœli data wydatku jest poza zakresem wyjazdu.
        /// </summary>
        [Fact]
        public async Task CreateExpense_ShouldReturnBadRequest_IfDateOutOfRange()
        {
            // Arrange
            SetUserContext(1);

            var trip = new Trip
            {
                TripId = 1,
                Name = "Test Trip",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-5),
                Description = "Test trip description",
                SecretCode = "1234",
                UserTrips = new List<UserTrip> { new UserTrip { UserId = 1 } }
            };

            _dbContext.Trips.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dto = new CreateExpenseModel
            {
                TripId = 1,
                Cost = 100,
                Currency = "USD",
                Date = DateTime.UtcNow, // Poza zakresem
                Description = "Invalid Expense",
                Category = "Food",
                Location = "City"
            };

            // Act
            var result = await _controller.CreateExpense(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Data wydatku musi mieœciæ siê w przedziale dat wyjazdu.", JObject.FromObject(badRequestResult.Value)["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda UpdateDebtStatus zwraca NotFound, jeœli d³ug nie istnieje.
        /// </summary>
        [Fact]
        public async Task UpdateDebtStatus_ShouldReturnNotFound_IfDebtDoesNotExist()
        {
            // Act
            var result = await _controller.UpdateDebtStatus(999, "paid");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("D³ug o podanym ID nie istnieje.", JObject.FromObject(notFoundResult.Value)["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda DeleteExpense zwraca NotFound, jeœli wydatek nie istnieje.
        /// </summary>
        [Fact]
        public async Task DeleteExpense_ShouldReturnNotFound_IfExpenseDoesNotExist()
        {
            // Arrange
            SetUserContext(1);

            // Act
            var result = await _controller.DeleteExpense(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Wydatek o podanym ID nie istnieje.", JObject.FromObject(notFoundResult.Value)["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda DeleteExpense zwraca BadRequest, jeœli u¿ytkownik nie jest twórc¹ wydatku.
        /// </summary>
        [Fact]
        public async Task DeleteExpense_ShouldReturnBadRequest_IfUserNotCreator()
        {
            // Arrange
            SetUserContext(1);

            var expense = new Expense
            {
                ExpenseId = 1,
                TripId = 1,
                CreatorId = 2, // Inny u¿ytkownik
                Cost = 100,
                Currency = "USD",
                Date = DateTime.UtcNow,
                Description = "Test Expense",
                Category = "Food",
                Location = "City"
            };

            _dbContext.Expenses.Add(expense);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteExpense(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nie masz uprawnieñ do usuniêcia tego wydatku.", JObject.FromObject(badRequestResult.Value)["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda GetDebtsSummaryByUser zwraca pust¹ listê, jeœli u¿ytkownik nie ma d³ugów.
        /// </summary>
        [Fact]
        public async Task GetDebtsSummaryByUser_ShouldReturnEmptyList_IfUserHasNoDebts()
        {
            // Arrange
            SetUserContext(1);

            // Act
            var result = await _controller.GetDebtsSummaryByUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = JObject.FromObject(okResult.Value);
            Assert.Equal("Nie masz ¿adnych aktywnych d³ugów.", response["message"].ToString());
        }

        /// <summary>
        /// Usuwa bazê danych po zakoñczeniu testów.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
