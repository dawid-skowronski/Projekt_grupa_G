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
    /// Klasa testowa sprawdzaj¹ca dodatkowe funkcje konta u¿ytkownika, 
    /// w szczególnoœci dostêpnoœæ nazwy u¿ytkownika.
    /// </summary>
    public class AccountOtherTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly AccountController _controller;

        /// <summary>
        /// Inicjalizuje testy, tworz¹c bazê danych w pamiêci i dodaj¹c testowego u¿ytkownika.
        /// </summary>
        public AccountOtherTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestCheckUsernameDb")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _controller = new AccountController(_dbContext, null);

            // Tworzymy testowego u¿ytkownika, którego nazwa u¿ytkownika jest zajêta
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
        /// Test sprawdzaj¹cy, czy system zwraca true, gdy nazwa u¿ytkownika jest dostêpna.
        /// Oczekiwane zachowanie: zwrócenie statusu 200 OK i wartoœci `isAvailable = true`.
        /// </summary>
        [Fact]
        public async Task CheckUsernameAvailability_ReturnsTrue_WhenUsernameIsAvailable()
        {
            // Act - Sprawdzenie dostêpnoœci nowej nazwy u¿ytkownika
            var result = await _controller.CheckUsernameAvailability("newuser") as OkObjectResult;

            // Assert - Sprawdzenie statusu odpowiedzi oraz dostêpnoœci
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.True((bool)result.Value.GetType().GetProperty("isAvailable").GetValue(result.Value));
        }

        /// <summary>
        /// Test sprawdzaj¹cy, czy system zwraca false, gdy nazwa u¿ytkownika jest ju¿ zajêta.
        /// Oczekiwane zachowanie: zwrócenie statusu 200 OK i wartoœci `isAvailable = false`.
        /// </summary>
        [Fact]
        public async Task CheckUsernameAvailability_ReturnsFalse_WhenUsernameIsNotAvailable()
        {
            // Act - Sprawdzenie dostêpnoœci nazwy u¿ytkownika, która ju¿ istnieje
            var result = await _controller.CheckUsernameAvailability("existinguser") as OkObjectResult;

            // Assert - Sprawdzenie statusu odpowiedzi oraz dostêpnoœci
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.False((bool)result.Value.GetType().GetProperty("isAvailable").GetValue(result.Value));
        }
    }
}
