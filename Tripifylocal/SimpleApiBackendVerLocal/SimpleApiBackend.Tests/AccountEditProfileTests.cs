using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AccountEditProfileTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AccountController _controller;

    /// <summary>
    /// Konstruktor inicjalizuj�cy testy dla edycji profilu.
    /// Tworzy baz� danych w pami�ci, mockuje konfiguracj� JWT i resetuje baz�.
    /// </summary>
    public AccountEditProfileTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestEditProfileDb")
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockConfig = new Mock<IConfiguration>();

        _mockConfig.Setup(c => c["Jwt:SecretKey"])
            .Returns("BardzoD�ugiSekretnyKluczJWT1234567890!@#$%^&*()");

        _controller = new AccountController(_dbContext, _mockConfig.Object);

        ResetDatabase();
        SetupFakeHttpContext();
    }

    /// <summary>
    /// Resetuje baz� danych przed ka�dym testem, usuwaj�c istniej�cych u�ytkownik�w.
    /// </summary>
    private void ResetDatabase()
    {
        _dbContext.Users.RemoveRange(_dbContext.Users);
        _dbContext.SaveChanges();

        var user = new User
        {
            Id = 1,
            Username = "existinguser",
            Email = "existing@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            IsEmailConfirmed = true
        };
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// Ustawia fa�szywy kontekst HTTP z tokenem JWT dla testowego u�ytkownika.
    /// </summary>
    private void SetupFakeHttpContext()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "existing@example.com"),
            new Claim(ClaimTypes.Name, "existinguser")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    /// <summary>
    /// Test sprawdzaj�cy, czy u�ytkownik mo�e pomy�lnie zmieni� nazw� u�ytkownika.
    /// Oczekiwane zachowanie: zwr�cenie statusu 200 OK oraz odpowiedniego komunikatu.
    /// </summary>
    [Fact]
    public async Task EditProfile_ReturnsOk_WhenUsernameIsUpdated()
    {
        // Arrange - reset bazy danych i ustawienie nowej nazwy u�ytkownika
        ResetDatabase();

        var model = new EditProfileModel
        {
            NewUsername = "newusername"
        };

        // Act - wywo�anie metody edycji profilu
        var result = await _controller.EditProfile(model) as OkObjectResult;

        // Assert - sprawdzenie odpowiedzi
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Profil zaktualizowany pomy�lnie", result.Value.ToString());
    }

    /// <summary>
    /// Test sprawdzaj�cy, czy edycja profilu zwr�ci b��d, gdy nowa nazwa u�ytkownika jest ju� zaj�ta.
    /// Oczekiwane zachowanie: zwr�cenie statusu 400 BadRequest z komunikatem o b��dzie.
    /// </summary>
    [Fact]
    public async Task EditProfile_ReturnsBadRequest_WhenUsernameIsTaken()
    {
        // Arrange - reset bazy i dodanie u�ytkownika z zaj�t� nazw�
        ResetDatabase();

        var existingUser = new User
        {
            Id = 2,
            Username = "takenusername",
            Email = "taken@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            IsEmailConfirmed = true
        };
        _dbContext.Users.Add(existingUser);
        _dbContext.SaveChanges();

        var model = new EditProfileModel
        {
            NewUsername = "takenusername"
        };

        // Act - pr�ba zmiany nazwy u�ytkownika na zaj�t�
        var result = await _controller.EditProfile(model) as BadRequestObjectResult;

        // Assert - sprawdzenie odpowiedzi
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Nazwa u�ytkownika jest ju� zaj�ta", result.Value.ToString());
    }
}
