using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System.Threading.Tasks;
using Xunit;

namespace SimpleApiBackend.Tests
{
    /// <summary>
    /// Klasa testowa sprawdzaj�ca dodatkowe funkcje konta u�ytkownika, 
    /// w szczeg�lno�ci dost�pno�� nazwy u�ytkownika.
    /// </summary>
    public class AccountOtherTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly AccountController _controller;

        /// <summary>
        /// Inicjalizuje testy, tworz�c baz� danych w pami�ci i dodaj�c testowego u�ytkownika.
        /// </summary>
        public AccountOtherTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestCheckUsernameDb")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _controller = new AccountController(_dbContext, null);

            // Tworzymy testowego u�ytkownika, kt�rego nazwa u�ytkownika jest zaj�ta
            var user = new User
            {
                Username = "existinguser",
                Email = "existing@example.com",
                Password = "Test123!"
            };
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
        }

        /// <summary>
        /// Test sprawdzaj�cy, czy system zwraca true, gdy nazwa u�ytkownika jest dost�pna.
        /// Oczekiwane zachowanie: zwr�cenie statusu 200 OK i warto�ci `isAvailable = true`.
        /// </summary>
        [Fact]
        public async Task CheckUsernameAvailability_ReturnsTrue_WhenUsernameIsAvailable()
        {
            // Act - Sprawdzenie dost�pno�ci nowej nazwy u�ytkownika
            var result = await _controller.CheckUsernameAvailability("newuser") as OkObjectResult;

            // Assert - Sprawdzenie statusu odpowiedzi oraz dost�pno�ci
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.True((bool)result.Value.GetType().GetProperty("isAvailable").GetValue(result.Value));
        }

        /// <summary>
        /// Test sprawdzaj�cy, czy system zwraca false, gdy nazwa u�ytkownika jest ju� zaj�ta.
        /// Oczekiwane zachowanie: zwr�cenie statusu 200 OK i warto�ci `isAvailable = false`.
        /// </summary>
        [Fact]
        public async Task CheckUsernameAvailability_ReturnsFalse_WhenUsernameIsNotAvailable()
        {
            // Act - Sprawdzenie dost�pno�ci nazwy u�ytkownika, kt�ra ju� istnieje
            var result = await _controller.CheckUsernameAvailability("existinguser") as OkObjectResult;

            // Assert - Sprawdzenie statusu odpowiedzi oraz dost�pno�ci
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.False((bool)result.Value.GetType().GetProperty("isAvailable").GetValue(result.Value));
        }
    }
}
