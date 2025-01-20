using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.IdentityModel.Tokens;

namespace SimpleApiBackend.Tests
{
    /// <summary>
    /// Klasa testowa dla rejestracji użytkownika w AccountController.
    /// </summary>
    public class AccountRegistrationTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AccountController _controller;

        /// <summary>
        /// Inicjalizuje testy, konfigurując bazę danych w pamięci i ustawienia JWT.
        /// </summary>
        public AccountRegistrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestRegisterDb")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _mockConfig = new Mock<IConfiguration>();

            // ✅ Ustawienie klucza JWT Email Secret Key
            _mockConfig.Setup(c => c["Jwt:EmailSecretKey"])
                       .Returns("InnySuperTajnyKluczJWTDoEmaila1234567890!@");
            _mockConfig.Setup(c => c["Jwt:Issuer"])
                       .Returns("SimpleApiBackend");
            _mockConfig.Setup(c => c["Jwt:Audience"])
                       .Returns("SimpleApiFrontend");

            _controller = new AccountController(_dbContext, _mockConfig.Object);
        }

        /// <summary>
        /// Test sprawdzający, czy rejestracja zwraca kod 200 OK, gdy dane są poprawne.
        /// Oczekiwane zachowanie: odpowiedź 200 i komunikat "Rejestracja zakończona sukcesem".
        /// </summary>
        [Fact]
        public async Task Register_ReturnsOk_WhenDataIsValid()
        {
            // Arrange
            var model = new RegisterModel
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "StrongPass1!",
                ConfirmPassword = "StrongPass1!"
            };

            // Act
            var result = await _controller.Register(model) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("Rejestracja zakończona sukcesem", result.Value.ToString());
        }

        /// <summary>
        /// Test sprawdzający, czy rejestracja zwraca kod 400 (BadRequest), gdy hasła nie pasują do siebie.
        /// Oczekiwane zachowanie: odpowiedź 400 i komunikat "Hasła nie pasują do siebie".
        /// </summary>
        [Fact]
        public async Task Register_ReturnsBadRequest_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var model = new RegisterModel
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "StrongPass1!",
                ConfirmPassword = "WrongPass1!"
            };

            // Act
            var result = await _controller.Register(model) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Hasła nie pasują do siebie", result.Value.ToString());
        }

        /// <summary>
        /// Test sprawdzający, czy metoda GenerateEmailConfirmationToken zwraca poprawny token JWT.
        /// Oczekiwane zachowanie: token jest niepusty i zawiera prawidłową datę wygaśnięcia.
        /// </summary>
        [Fact]
        public void GenerateEmailConfirmationToken_ReturnsValidToken()
        {
            // Arrange - Pobranie metody prywatnej GenerateEmailConfirmationToken
            var method = typeof(AccountController)
                .GetMethod("GenerateEmailConfirmationToken", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var user = new User { Id = 1, Email = "test@example.com" };

            // Act - Wywołanie metody i uzyskanie tokenu
            var token = method.Invoke(_controller, new object[] { user }) as string;

            // Assert - Sprawdzenie czy token jest prawidłowy
            Assert.NotNull(token);
            Assert.IsType<string>(token);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Console.WriteLine($"🔍 Debug: Odczytany token: {token}");
            Console.WriteLine($"🔍 Debug: Claims w tokenie:");
            foreach (var claim in jwtToken.Claims)
            {
                Console.WriteLine($"   👉 {claim.Type} => {claim.Value}");
            }

            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
            Assert.NotNull(expClaim);
            Assert.NotEmpty(expClaim.Value);

            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value)).UtcDateTime;
            Console.WriteLine($"🔍 Debug: Odczytana data wygaśnięcia: {expirationTime}");

            Assert.True(expirationTime > DateTime.UtcNow, "Token nie ma poprawnej daty wygaśnięcia!");
        }

        /// <summary>
        /// Test sprawdzający, czy token wygenerowany przez GenerateEmailConfirmationToken zawiera poprawną datę wygaśnięcia.
        /// Oczekiwane zachowanie: `exp` w tokenie istnieje i jest większy niż aktualny czas.
        /// </summary>
        [Fact]
        public void GenerateEmailConfirmationToken_ShouldContainExpiration()
        {
            // Arrange - Pobranie metody prywatnej
            var method = typeof(AccountController)
                .GetMethod("GenerateEmailConfirmationToken", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var user = new User { Id = 1, Email = "test@example.com" };

            // Act - Generowanie tokenu
            var token = method.Invoke(_controller, new object[] { user }) as string;
            Assert.NotNull(token);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // 🔥 Nowy sposób odczytu daty wygaśnięcia 🔥
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            Assert.NotNull(expClaim);

            var expUnixTime = long.Parse(expClaim);
            var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;

            Console.WriteLine($"✅ Debug: Odczytana data wygaśnięcia: {expDateTime}");

            // Assert - Sprawdzenie, czy data wygaśnięcia jest w przyszłości
            Assert.True(expDateTime > DateTime.UtcNow, "Token nie zawiera poprawnej daty wygaśnięcia!");
        }

        /// <summary>
        /// Sprawdza, czy rejestracja zwraca kod 400 (BadRequest), gdy użytkownik o podanym e-mailu lub nazwie użytkownika już istnieje.
        /// </summary>
        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUserAlreadyExists()
        {
            // ARRANGE: Dodanie użytkownika do bazy przed rejestracją.
            var existingUser = new User
            {
                Username = "existinguser",
                Email = "existing@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("StrongPass1!")
            };
            _dbContext.Users.Add(existingUser);
            await _dbContext.SaveChangesAsync();

            var model = new RegisterModel
            {
                Username = "existinguser",
                Email = "existing@example.com",
                Password = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };

            // ACT: Próba rejestracji z tymi samymi danymi.
            var result = await _controller.Register(model) as BadRequestObjectResult;

            // ASSERT: Powinno zwrócić kod 400 i odpowiedni komunikat o duplikacie.
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Użytkownik o takim loginie lub emailu już istnieje.", result.Value.ToString());
        }

        /// <summary>
        /// Sprawdza, czy rejestracja zwraca kod 400 (BadRequest), gdy hasło jest zbyt słabe.
        /// </summary>
        [Fact]
        public async Task Register_ReturnsBadRequest_WhenPasswordIsWeak()
        {
            // ARRANGE: Próba rejestracji z za słabym hasłem.
            var model = new RegisterModel
            {
                Username = "weakpassworduser",
                Email = "weakpass@example.com",
                Password = "1234",
                ConfirmPassword = "1234"
            };

            // ACT: Próba rejestracji.
            var result = await _controller.Register(model) as BadRequestObjectResult;

            // ASSERT: Powinno zwrócić kod 400 i odpowiedni komunikat.
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Hasło musi zawierać co najmniej 8 znaków", result.Value.ToString());
        }

        /// <summary>
        /// Sprawdza, czy rejestracja zwraca kod 400 (BadRequest), gdy brakuje wymaganych pól.
        /// </summary>
        [Fact]
        public async Task Register_ReturnsBadRequest_WhenRequiredFieldsAreMissing()
        {
            // ARRANGE: Użytkownik nie podaje hasła.
            var model = new RegisterModel
            {
                Username = "nouserpassword",
                Email = "nouserpass@example.com",
                Password = "",
                ConfirmPassword = ""
            };

            // ACT: Próba rejestracji.
            var result = await _controller.Register(model) as BadRequestObjectResult;

            // ASSERT: Powinno zwrócić kod 400 i komunikat o błędnych danych.
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("Hasło musi zawierać co najmniej 8 znaków", result.Value.ToString());
        }




    }
}
