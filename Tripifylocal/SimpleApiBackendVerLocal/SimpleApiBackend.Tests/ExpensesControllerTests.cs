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
    /// Testy jednostkowe dla kontrolera ExpensesController.
    /// </summary>
    public class ExpensesControllerTests : IDisposable
    {
        private ApplicationDbContext _dbContext;
        private ExpensesController _controller;

        /// <summary>
        /// Tworzy now� instancj� test�w ExpensesControllerTests, konfiguruj�c baz� danych w pami�ci.
        /// </summary>
        public ExpensesControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _controller = new ExpensesController(_dbContext);
        }

        /// <summary>
        /// Ustawia kontekst u�ytkownika dla test�w wymagaj�cych autoryzacji.
        /// </summary>
        /// <param name="userId">Identyfikator u�ytkownika.</param>
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
        /// Sprawdza, czy metoda CreateExpense zwraca BadRequest, gdy przekazany DTO jest null.
        /// </summary>
        [Fact]
        public async Task CreateExpense_ShouldReturnBadRequest_IfDtoIsNull()
        {
            // ARRANGE: Nie przekazujemy �adnych danych (null).

            // ACT: Wywo�anie metody CreateExpense z null jako argumentem.
            var result = await _controller.CreateExpense(null);

            // ASSERT: Powinno zwr�ci� BadRequestObjectResult z odpowiednim komunikatem.
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            var response = JObject.FromObject(badRequestResult.Value);
            Assert.Equal("Nieprawid�owe dane wydatku.", response["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda CreateExpense zwraca Unauthorized, gdy u�ytkownik nie jest zalogowany.
        /// </summary>
        [Fact]
        public async Task CreateExpense_ShouldReturnUnauthorized_IfUserNotLoggedIn()
        {
            // ARRANGE: Usuni�cie kontekstu u�ytkownika, aby zasymulowa� brak autoryzacji.
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // ACT: Wywo�anie metody CreateExpense bez zalogowanego u�ytkownika.
            var result = await _controller.CreateExpense(new CreateExpenseModel());

            // ASSERT: Powinno zwr�ci� UnauthorizedObjectResult z odpowiednim komunikatem.
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);

            var response = JObject.FromObject(unauthorizedResult.Value);
            Assert.Equal("Brak uprawnie�. U�ytkownik nie jest zalogowany.", response["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda CreateExpense zwraca NotFound, gdy podr� o podanym ID nie istnieje.
        /// </summary>
        [Fact]
        public async Task CreateExpense_ShouldReturnNotFound_IfTripDoesNotExist()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika
            SetUserContext(1);

            var dto = new CreateExpenseModel { TripId = 1 }; // Podr� nie istnieje w bazie

            // ACT: Pr�ba utworzenia wydatku dla nieistniej�cej podr�y
            var result = await _controller.CreateExpense(dto);

            // ASSERT: Powinno zwr�ci� NotFoundObjectResult z odpowiednim komunikatem.
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);

            var response = JObject.FromObject(notFoundResult.Value);
            Assert.Equal("Podr� nie istnieje.", response["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda CreateExpense zwraca BadRequest, gdy u�ytkownik nie jest cz�ci� podr�y.
        /// </summary>
        [Fact]
        public async Task CreateExpense_ShouldReturnBadRequest_IfUserNotPartOfTrip()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika
            SetUserContext(1);

            // Tworzenie podr�y bez u�ytkownik�w
            var trip = new Trip
            {
                TripId = 1,
                Name = "Test Trip",
                Description = "Test Description",
                SecretCode = "1234",
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                UserTrips = new List<UserTrip>() // Brak u�ytkownik�w przypisanych do podr�y
            };

            _dbContext.Trips.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dto = new CreateExpenseModel { TripId = 1 };

            // ACT: Pr�ba utworzenia wydatku przez u�ytkownika, kt�ry nie nale�y do podr�y
            var result = await _controller.CreateExpense(dto);

            // ASSERT: Powinno zwr�ci� BadRequestObjectResult z odpowiednim komunikatem.
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            var response = JObject.FromObject(badRequestResult.Value);
            Assert.Equal("Tw�rca nie nale�y do tej podr�y.", response["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda CreateExpense poprawnie tworzy wydatek, gdy spe�nione s� wszystkie warunki.
        /// </summary>
        [Fact]
        public async Task CreateExpense_ShouldCreateExpense_IfAllConditionsMet()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika oraz utworzenie podr�y z u�ytkownikiem.
            SetUserContext(1);

            var trip = new Trip
            {
                TripId = 1,
                Name = "Test Trip",
                Description = "Test Description",
                SecretCode = "1234",
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                UserTrips = new List<UserTrip> { new UserTrip { UserId = 1 } }
            };

            _dbContext.Trips.Add(trip);
            await _dbContext.SaveChangesAsync();

            var dto = new CreateExpenseModel
            {
                TripId = 1,
                Cost = 100,
                Currency = "USD",
                Date = DateTime.UtcNow,
                Description = "Test expense",
                Category = "Food",
                Location = "City"
            };

            // ACT: Wywo�anie metody CreateExpense.
            var result = await _controller.CreateExpense(dto);

            // ASSERT: Powinno zwr�ci� OkObjectResult z odpowiednim komunikatem.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = JObject.FromObject(okResult.Value);
            Assert.Contains("Wydatek i d�ugi zapisane pomy�lnie.", response["message"].ToString());

            // Sprawdzenie, czy wydatek zosta� zapisany w bazie danych.
            var savedExpense = _dbContext.Expenses.FirstOrDefault();
            Assert.NotNull(savedExpense);
            Assert.Equal(100, savedExpense.Cost);
        }

        /// <summary>
        /// Sprawdza, czy metoda UpdateDebtStatus poprawnie aktualizuje status d�ugu, gdy d�ug istnieje.
        /// </summary>
        [Fact]
        public async Task UpdateDebtStatus_ShouldUpdateStatus_IfDebtExists()
        {
            // ARRANGE: Utworzenie d�ugu w bazie danych.
            var debt = new Debt
            {
                DebtId = 1,
                ExpenseId = 1,
                UserId = 2,
                Amount = 50,
                Currency = "USD",
                Status = "unpaid"
            };

            _dbContext.Debts.Add(debt);
            await _dbContext.SaveChangesAsync();

            // ACT: Wywo�anie metody UpdateDebtStatus.
            var result = await _controller.UpdateDebtStatus(1, "paid");

            // ASSERT: Powinno zwr�ci� OkObjectResult z odpowiednim komunikatem.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = JObject.FromObject(okResult.Value);
            Assert.Equal("Status d�ugu zosta� zaktualizowany.", response["message"].ToString());

            // Sprawdzenie, czy status d�ugu zosta� zaktualizowany w bazie danych.
            var updatedDebt = _dbContext.Debts.First();
            Assert.Equal("paid", updatedDebt.Status);
        }

        /// <summary>
        /// Usuwa baz� danych po zako�czeniu test�w.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

    }
}
