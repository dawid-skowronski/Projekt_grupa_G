using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace SimpleApiBackend.Tests
{
    /// <summary>
    /// Klasa testowa sprawdzaj¹ca funkcjonalnoœæ resetowania has³a w kontrolerze konta.
    /// </summary>
    public class AccountControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AccountController _controller;

        /// <summary>
        /// Inicjalizuje testy, konfiguruj¹c bazê danych w pamiêci i ustawienia JWT.
        /// </summary>
        public AccountControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestResetPasswordDb")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(c => c["Jwt:EmailSecretKey"])
                       .Returns("InnySuperTajnyKluczJWTDoEmaila1234567890!@");

            _controller = new AccountController(_dbContext, _mockConfig.Object);
            ResetDatabase();
        }

        /// <summary>
        /// Resetuje bazê danych, usuwaj¹c istniej¹cych u¿ytkowników i dodaj¹c nowego u¿ytkownika testowego.
        /// </summary>
        private void ResetDatabase()
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
            _dbContext.SaveChanges();

            var user = new User
            {
                Id = 1,
                Username = "resetuser",
                Email = "reset@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                IsEmailConfirmed = true
            };
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
        }

        /// <summary>
        /// Test sprawdzaj¹cy, czy resetowanie has³a zwraca b³¹d 400 (BadRequest) w przypadku nieprawid³owego tokenu.
        /// Oczekiwane zachowanie: odpowiedŸ 400 z komunikatem "Nieprawid³owy lub wygas³y token resetuj¹cy."
        /// </summary>
        [Fact]
        public async Task ResetPassword_ShouldReturnBadRequest_IfTokenInvalid()
        {
            // Arrange
            var model = new ResetPasswordModel { Token = "invalid-token", NewPassword = "NewTest123!" };

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nieprawid³owy lub wygas³y token resetuj¹cy.", JObject.FromObject(badRequestResult.Value)["message"].ToString());
        }

        /// <summary>
        /// Test sprawdzaj¹cy, czy resetowanie has³a dzia³a poprawnie, gdy token jest prawid³owy.
        /// Oczekiwane zachowanie: odpowiedŸ 200 OK i poprawnie zapisane nowe has³o.
        /// </summary>
        [Fact]
        public async Task ResetPassword_ShouldResetPassword_IfTokenValid()
        {
            // Arrange - Pobranie u¿ytkownika testowego i wygenerowanie tokenu JWT
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "reset@example.com");
            var token = GenerateResetPasswordToken(user);

            var model = new ResetPasswordModel { Token = token, NewPassword = "NewTest123!" };

            // Act - Próba resetowania has³a
            var result = await _controller.ResetPassword(model);

            // Assert - Sprawdzenie statusu odpowiedzi i czy has³o faktycznie zosta³o zmienione
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Has³o zosta³o zresetowane pomyœlnie.", JObject.FromObject(okResult.Value)["message"].ToString());

            var updatedUser = await _dbContext.Users.FindAsync(user.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewTest123!", updatedUser.Password));
        }

        /// <summary>
        /// Generuje token JWT dla resetowania has³a u¿ytkownika.
        /// </summary>
        /// <param name="user">U¿ytkownik, dla którego generowany jest token.</param>
        /// <returns>Token JWT jako string.</returns>
        private string GenerateResetPasswordToken(User user)
        {
            var keyString = _mockConfig.Object["Jwt:EmailSecretKey"];
            var key = Encoding.UTF8.GetBytes(keyString);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Zwolnienie zasobów i usuniêcie bazy danych po zakoñczeniu testów.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
