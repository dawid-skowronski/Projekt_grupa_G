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
    /// Konstruktor inicjalizuj¹cy testy dla edycji profilu.
    /// Tworzy bazê danych w pamiêci, mockuje konfiguracjê JWT i resetuje bazê.
    /// </summary>
    public AccountEditProfileTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestEditProfileDb")
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockConfig = new Mock<IConfiguration>();

        _mockConfig.Setup(c => c["Jwt:SecretKey"])
            .Returns("BardzoD³ugiSekretnyKluczJWT1234567890!@#$%^&*()");

        _controller = new AccountController(_dbContext, _mockConfig.Object);

        ResetDatabase();
        SetupFakeHttpContext();
    }

    /// <summary>
    /// Resetuje bazê danych przed ka¿dym testem, usuwaj¹c istniej¹cych u¿ytkowników.
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
    /// Ustawia fa³szywy kontekst HTTP z tokenem JWT dla testowego u¿ytkownika.
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
    /// Test sprawdzaj¹cy, czy u¿ytkownik mo¿e pomyœlnie zmieniæ nazwê u¿ytkownika.
    /// Oczekiwane zachowanie: zwrócenie statusu 200 OK oraz odpowiedniego komunikatu.
    /// </summary>
    [Fact]
    public async Task EditProfile_ReturnsOk_WhenUsernameIsUpdated()
    {
        // Arrange - reset bazy danych i ustawienie nowej nazwy u¿ytkownika
        ResetDatabase();

        var model = new EditProfileModel
        {
            NewUsername = "newusername"
        };

        // Act - wywo³anie metody edycji profilu
        var result = await _controller.EditProfile(model) as OkObjectResult;

        // Assert - sprawdzenie odpowiedzi
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Profil zaktualizowany pomyœlnie", result.Value.ToString());
    }

    /// <summary>
    /// Test sprawdzaj¹cy, czy edycja profilu zwróci b³¹d, gdy nowa nazwa u¿ytkownika jest ju¿ zajêta.
    /// Oczekiwane zachowanie: zwrócenie statusu 400 BadRequest z komunikatem o b³êdzie.
    /// </summary>
    [Fact]
    public async Task EditProfile_ReturnsBadRequest_WhenUsernameIsTaken()
    {
        // Arrange - reset bazy i dodanie u¿ytkownika z zajêt¹ nazw¹
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

        // Act - próba zmiany nazwy u¿ytkownika na zajêt¹
        var result = await _controller.EditProfile(model) as BadRequestObjectResult;

        // Assert - sprawdzenie odpowiedzi
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Nazwa u¿ytkownika jest ju¿ zajêta", result.Value.ToString());
    }
}
