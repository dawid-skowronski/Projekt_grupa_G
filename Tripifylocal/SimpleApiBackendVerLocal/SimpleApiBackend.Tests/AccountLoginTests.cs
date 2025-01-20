using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Klasa testowa do sprawdzania logowania u�ytkownika.
/// Testuje poprawno�� logowania na podstawie r�nych scenariuszy.
/// </summary>
public class AccountLoginTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AccountController _controller;

    /// <summary>
    /// Inicjalizuje testy logowania.
    /// Tworzy baz� danych w pami�ci oraz mockuje konfiguracj� JWT.
    /// </summary>
    public AccountLoginTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestLoginDb")
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockConfig = new Mock<IConfiguration>();

        // Ustawienia JWT
        _mockConfig.Setup(c => c["Jwt:SecretKey"]).Returns("ThisIsASecretKeyForJwtToken1234567890");
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _controller = new AccountController(_dbContext, _mockConfig.Object);

        ResetDatabase();
    }

    /// <summary>
    /// Resetuje baz� danych i dodaje domy�lnego u�ytkownika testowego.
    /// </summary>
    private void ResetDatabase()
    {
        _dbContext.Users.RemoveRange(_dbContext.Users);
        _dbContext.SaveChanges();

        _dbContext.Users.Add(new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            IsEmailConfirmed = true
        });
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// Test sprawdza, czy u�ytkownik mo�e zalogowa� si� poprawnymi danymi.
    /// Oczekiwane zachowanie: zwr�cenie statusu 200 OK oraz komunikatu o pomy�lnym logowaniu.
    /// </summary>
    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        // Arrange - Reset bazy i utworzenie modelu logowania
        ResetDatabase();

        var model = new LoginModel
        {
            Username = "testuser",
            Password = "Test123!"
        };

        // Act - Wywo�anie logowania
        var result = await _controller.Login(model) as OkObjectResult;

        // Assert - Sprawdzenie statusu odpowiedzi
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Logowanie udane", result.Value.ToString());
    }

    /// <summary>
    /// Test sprawdza, czy u�ytkownik otrzyma b��d autoryzacji, je�li poda z�e has�o.
    /// Oczekiwane zachowanie: zwr�cenie statusu 401 Unauthorized i komunikatu o b��dnych danych.
    /// </summary>
    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange - Reset bazy i model z niepoprawnym has�em
        ResetDatabase();

        var model = new LoginModel
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        // Act - Pr�ba logowania
        var result = await _controller.Login(model) as UnauthorizedObjectResult;

        // Assert - Sprawdzenie statusu odpowiedzi
        Assert.NotNull(result);
        Assert.Equal(401, result.StatusCode);
        Assert.Contains("Nieprawid�owe dane logowania", result.Value.ToString());
    }

    /// <summary>
    /// Test sprawdza, czy u�ytkownik, kt�ry nie potwierdzi� konta, nie mo�e si� zalogowa�.
    /// Oczekiwane zachowanie: zwr�cenie statusu 401 Unauthorized i komunikatu o konieczno�ci potwierdzenia konta.
    /// </summary>
    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailNotConfirmed()
    {
        // Arrange - Reset bazy i dodanie u�ytkownika bez potwierdzenia e-maila
        ResetDatabase();

        var unconfirmedUser = new User
        {
            Id = 2,
            Username = "unconfirmedUser",
            Email = "unconfirmed@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            IsEmailConfirmed = false
        };
        _dbContext.Users.Add(unconfirmedUser);
        _dbContext.SaveChanges();

        var model = new LoginModel
        {
            Username = "unconfirmedUser",
            Password = "Test123!"
        };

        // Act - Pr�ba logowania u�ytkownika bez potwierdzonego e-maila
        var result = await _controller.Login(model) as UnauthorizedObjectResult;

        // Assert - Sprawdzenie statusu odpowiedzi
        Assert.NotNull(result);
        Assert.Equal(401, result.StatusCode);
        Assert.Contains("Potwierd� swoje konto przed zalogowaniem", result.Value.ToString());
    }
}
