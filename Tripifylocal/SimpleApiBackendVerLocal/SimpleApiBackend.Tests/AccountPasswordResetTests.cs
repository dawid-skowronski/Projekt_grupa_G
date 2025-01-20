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
    /// Klasa testowa sprawdzaj�ca funkcjonalno�� resetowania has�a w kontrolerze konta.
    /// </summary>
    public class AccountControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AccountController _controller;

        /// <summary>
        /// Inicjalizuje testy, konfiguruj�c baz� danych w pami�ci i ustawienia JWT.
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
        /// Resetuje baz� danych, usuwaj�c istniej�cych u�ytkownik�w i dodaj�c nowego u�ytkownika testowego.
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
        /// Test sprawdzaj�cy, czy resetowanie has�a zwraca b��d 400 (BadRequest) w przypadku nieprawid�owego tokenu.
        /// Oczekiwane zachowanie: odpowied� 400 z komunikatem "Nieprawid�owy lub wygas�y token resetuj�cy."
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
            Assert.Equal("Nieprawid�owy lub wygas�y token resetuj�cy.", JObject.FromObject(badRequestResult.Value)["message"].ToString());
        }

        /// <summary>
        /// Test sprawdzaj�cy, czy resetowanie has�a dzia�a poprawnie, gdy token jest prawid�owy.
        /// Oczekiwane zachowanie: odpowied� 200 OK i poprawnie zapisane nowe has�o.
        /// </summary>
        [Fact]
        public async Task ResetPassword_ShouldResetPassword_IfTokenValid()
        {
            // Arrange - Pobranie u�ytkownika testowego i wygenerowanie tokenu JWT
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "reset@example.com");
            var token = GenerateResetPasswordToken(user);

            var model = new ResetPasswordModel { Token = token, NewPassword = "NewTest123!" };

            // Act - Pr�ba resetowania has�a
            var result = await _controller.ResetPassword(model);

            // Assert - Sprawdzenie statusu odpowiedzi i czy has�o faktycznie zosta�o zmienione
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Has�o zosta�o zresetowane pomy�lnie.", JObject.FromObject(okResult.Value)["message"].ToString());

            var updatedUser = await _dbContext.Users.FindAsync(user.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewTest123!", updatedUser.Password));
        }

        /// <summary>
        /// Generuje token JWT dla resetowania has�a u�ytkownika.
        /// </summary>
        /// <param name="user">U�ytkownik, dla kt�rego generowany jest token.</param>
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
        /// Zwolnienie zasob�w i usuni�cie bazy danych po zako�czeniu test�w.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
