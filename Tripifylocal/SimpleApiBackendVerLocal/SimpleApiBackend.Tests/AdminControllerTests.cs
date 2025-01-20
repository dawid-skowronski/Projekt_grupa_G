using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SimpleApiBackend.Tests
{
    /// <summary>
    /// Klasa testowa dla <see cref="AdminController"/>, zawierająca testy CRUD użytkowników i podróży.
    /// </summary>
    public class AdminControllerTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly AdminController _controller;

        /// <summary>
        /// Inicjalizuje testy, tworząc bazę danych w pamięci i wypełniając ją przykładowymi danymi.
        /// </summary>
        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestAdminDb")
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _controller = new AdminController(_dbContext);

            SeedDatabase().Wait();
        }

        /// <summary>
        /// Wypełnia bazę danych przykładowymi użytkownikami i podróżami.
        /// </summary>
        private async Task SeedDatabase()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();

            _dbContext.Users.AddRange(new List<User>
            {
                new User { Id = 1, Username = "admin", Email = "admin@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Test123!"), Role = "Admin" },
                new User { Id = 2, Username = "testuser", Email = "testuser@example.com", Password = BCrypt.Net.BCrypt.HashPassword("Test123!"), Role = "User" }
            });

            _dbContext.Trips.Add(new Trip
            {
                TripId = 1,
                Name = "Test Trip",
                Description = "Test Description",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                SecretCode = "ABC123"
            });

            _dbContext.UserTrips.Add(new UserTrip { UserId = 2, TripId = 1 });

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Deserializuje odpowiedź kontrolera do formatu Dictionary.
        /// </summary>
        private static Dictionary<string, object> DeserializeResponse(object responseValue)
        {
            try
            {
                if (responseValue is string str)
                {
                    return new Dictionary<string, object> { { "message", str } };
                }
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(responseValue,
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                );
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Test sprawdzający, czy metoda GetUsers zwraca listę użytkowników.
        /// </summary>
        [Fact]
        public async Task GetUsers_ReturnsListOfUsers()
        {
            var result = await _controller.GetUsers() as OkObjectResult;
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(200, result.StatusCode);
            var users = result.Value as List<User>;
            Assert.NotNull(users);
            Assert.Equal(2, users.Count);
        }

        /// <summary>
        /// Test sprawdzający, czy metoda UpdateUserRole poprawnie aktualizuje rolę użytkownika.
        /// </summary>
        [Fact]
        public async Task UpdateUserRole_UserExists_UpdatesRole()
        {
            var result = await _controller.UpdateUserRole(2, "Moderator");
            var response = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(response.Value);
            var responseData = DeserializeResponse(response.Value);
            Assert.Contains("message", responseData);
            Assert.Equal("Role updated successfully.", responseData["message"]);
            var updatedUser = await _dbContext.Users.FindAsync(2);
            Assert.Equal("Moderator", updatedUser.Role);
        }

        /// <summary>
        /// Test sprawdzający, czy metoda DeleteUser poprawnie usuwa użytkownika.
        /// </summary>
        [Fact]
        public async Task DeleteUser_UserExists_DeletesUser()
        {
            var result = await _controller.DeleteUser(2);
            var response = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(response.Value);
            var responseData = DeserializeResponse(response.Value);
            Assert.Contains("message", responseData);
            Assert.Equal("User deleted successfully", responseData["message"]);
            var deletedUser = await _dbContext.Users.FindAsync(2);
            Assert.Null(deletedUser);
        }

        /// <summary>
        /// Test sprawdzający, czy metoda EditTrip poprawnie aktualizuje podróż.
        /// </summary>
        [Fact]
        public async Task EditTrip_TripExists_UpdatesTrip()
        {
            var updatedTrip = new TripCreateModel
            {
                Name = "Updated Trip",
                Description = "Updated Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(7)
            };

            var result = await _controller.EditTrip(1, updatedTrip);
            var response = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(response.Value);
            var responseData = DeserializeResponse(response.Value);
            Assert.Contains("message", responseData);
            Assert.Equal("Trip updated successfully.", responseData["message"]);
            var trip = await _dbContext.Trips.FindAsync(1);
            Assert.Equal("Updated Trip", trip.Name);
            Assert.Equal("Updated Description", trip.Description);
        }

        /// <summary>
        /// Test sprawdzający, czy metoda GetUserTrips zwraca podróże użytkownika.
        /// </summary>
        [Fact]
        public async Task GetUserTrips_UserHasTrips_ReturnsTrips()
        {
            _dbContext.ChangeTracker.Clear();

            var user = await _dbContext.Users.FindAsync(2);
            Assert.NotNull(user);

            var result = await _controller.GetUserTrips(user.Id);

            Assert.NotNull(result);
            var response = Assert.IsType<OkObjectResult>(result);

            Assert.NotNull(response.Value);

            Console.WriteLine($"[DEBUG] Odpowiedź kontrolera: {JsonConvert.SerializeObject(response.Value)}");

            var trips = JsonConvert.DeserializeObject<List<Trip>>(JsonConvert.SerializeObject(response.Value));

            Assert.NotNull(trips);
            Assert.Single(trips);

            var trip = trips.First();
            Assert.Equal(1, trip.TripId);
            Assert.Equal("Test Trip", trip.Name);
            Assert.Equal("Test Description", trip.Description);
        }
    }
}
