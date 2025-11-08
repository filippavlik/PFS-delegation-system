using AdminPartDevelop.Common;
using AdminPartDevelop.Controllers;
using AdminPartDevelop.Data;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Hubs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Services.AdminServices;
using AdminPartDevelop.Services.EmailsSender;
using AdminPartDevelop.Services.FileParsers;
using AdminPartDevelop.Services.RefereeServices;
using AdminPartDevelop.Services.RouteServices;
using AdminPartDevelop.TestData;
using AdminPartDevelop.Views.ViewModels;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AdminPartDevelop.Tests.Controllers
{
    public class RefereeControllerTest : IDisposable
    {
        private readonly AdminDbContext _adminContext;
        private readonly RefereeDbContext _refereeContext;
        private readonly RefereeController _controller;
        private readonly AdminRepo _adminRepo;
        private readonly RefereeRepo _refereeRepo;
        private readonly IAdminService _adminService;
        private readonly IRefereeService _refereeService;
        private readonly IExcelParser _excelParser;
        private readonly Mock<IHubContext<HubForReendering>> _mockHubContext;
        private readonly Mock<IGeocodingService> _mockCar;
        private readonly Mock<IRouteBusPlanner> _mockBus;
        private readonly Mock<IEmailsToLoginDbSender> _emailsSender;
        private readonly IMemoryCache _memoryCache;
        private const string MatchesCacheKey = "AppMatches";


        public RefereeControllerTest()
        {
            var optionsAdmin = new DbContextOptionsBuilder<AdminDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _adminContext = new AdminDbContext(optionsAdmin);

            var optionsReferee = new DbContextOptionsBuilder<RefereeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _refereeContext = new RefereeDbContext(optionsReferee);

            // Seed databases
            SeedDatabase();

            // Create mocks
            var mockAdminRepoLogger = new Mock<ILogger<AdminRepo>>();
            var mockRefereeRepoLogger = new Mock<ILogger<RefereeRepo>>();

            // Create repositories
            _adminRepo = new AdminRepo(mockAdminRepoLogger.Object, _adminContext);
            _refereeRepo = new RefereeRepo(mockRefereeRepoLogger.Object, _refereeContext);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            var mockControllerLogger = new Mock<ILogger<RefereeController>>();
            var mockAdminServiceLogger = new Mock<ILogger<AdminService>>();
            var mockRefereeServiceLogger = new Mock<ILogger<RefereeService>>();
            var mockGetDataServiceLogger = new Mock<ILogger<GetData>>();


            _mockCar = new Mock<IGeocodingService>();
            _mockBus = new Mock<IRouteBusPlanner>();
            _emailsSender = new Mock<IEmailsToLoginDbSender>();
    




            _adminService = new AdminService(_adminRepo,mockAdminServiceLogger.Object);
            _refereeService = new RefereeService(_adminRepo, _mockCar.Object, _mockBus.Object, mockRefereeServiceLogger.Object);
            _excelParser = new GetData(mockGetDataServiceLogger.Object);
            _mockHubContext = new Mock<IHubContext<HubForReendering>>();



            // Create controller
            _controller = new RefereeController(
                _refereeRepo,
                _adminRepo,             
                _excelParser,
                _emailsSender.Object,
                _refereeService,
                _adminService,
                _mockHubContext.Object,
                _memoryCache,
                mockControllerLogger.Object
            );
        }

        private void SeedDatabase()
        {
            // Seed admin database
            _adminContext.StartingGameDates.AddRange(MockedDbTestData.GetStartingGameDates());
            _adminContext.Competitions.AddRange(MockedDbTestData.GetCompetitions());
            _adminContext.Fields.AddRange(MockedDbTestData.GetFields());
            _adminContext.Teams.AddRange(MockedDbTestData.GetTeams());
            _adminContext.Matches.AddRange(MockedDbTestData.GetMatches());
            _adminContext.Transfers.AddRange(MockedDbTestData.GetTransfers());
            _adminContext.Vetoes.AddRange(MockedDbTestData.GetVetoes());
            _adminContext.FilesPreviousDelegations.AddRange(MockedDbTestData.GetFilesPreviousDelegation());
            _adminContext.SaveChanges();

            // Seed referee database
            _refereeContext.Referees.AddRange(MockedDbTestData.GetReferees());
            _refereeContext.Excuses.AddRange(MockedDbTestData.GetExcuses());
            _refereeContext.VehicleSlots.AddRange(MockedDbTestData.GetVehicleSlots());
            _refereeContext.SaveChanges();
        }
        [Fact]
        public async Task AddNewRefereeAsync_WithValidRequest_ReturnsOkResult()
        {
            this._emailsSender
            .Setup(sender => sender.AddEmailsToAllowedListAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(ServiceResult<bool>.Success(true)); 

            // Arrange
            var request = new RefereeAddRequest
            {
                FacrId = "NEW001",
                Name = "Jan",
                Surname = "Novák",
                Email = "jan.novak@test.cz",
                League = 2,
                Age = 1985,
                Ofs = true,
                Note = "Test referee",
                CarAvailability = true,
                Place = "Praha 1"
            };

            // Act
            var result = await _controller.AddNewRefereeAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify referee was added to database
            var addedReferees = await _refereeRepo.GetRefereesAsync();
            var newReferee = addedReferees.GetDataOrThrow().FirstOrDefault(r => r.FacrId == "NEW001");
            Assert.NotNull(newReferee);
            Assert.Equal("Jan", newReferee.Name);
            Assert.Equal("Novák", newReferee.Surname);
        }

        [Fact]
        public async Task AddNewRefereeAsync_WithInvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new RefereeAddRequest
            {
                FacrId = "NEW002",
                Name = "Petr",
                Surname = "Test",
                Email = "invalid-email",
                League = 2,
                Age = 25,
                Ofs = true,
                CarAvailability = true
            };

            // Act
            var result = await _controller.AddNewRefereeAsync(request);

            Assert.IsType<BadRequestObjectResult>(result);

        }

        [Fact]
        public async Task AddRefereeToTheMatch_WithValidData_ReturnsOkResult()
        {
            var mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients.All).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
           
            // Arrange
            int refereeId = 2; 
            string matchId = "2023110A2B0404"; 
            int role = 1; 
            bool force = false;
            string user = "test_user";

            // Act
            var result = await _controller.AddRefereeToTheMatch(refereeId, matchId, role, force, user);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify match was updated
            var updatedMatch = await _adminRepo.GetMatchByIdAsync(matchId);
            var match = updatedMatch.GetDataOrThrow();
            Assert.Equal(refereeId, match.Ar1Id);
        }

        [Fact]
        public async Task AddRefereeToTheMatch_WithNonExistentReferee_ReturnsServerError()
        {
            // Arrange
            int refereeId = 999; // Non-existent referee
            string matchId = "2023110A2B0404";
            int role = 1;
            bool force = false;
            string user = "test_user";

            // Act
            var result = await _controller.AddRefereeToTheMatch(refereeId, matchId, role, force, user);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task AddRefereeToTheMatch_WithForceTrue_BypassesValidation()
        {
            var mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients.All).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
            _mockCar
                .Setup(route => route.CalculateRoute(
                    It.IsAny<float>(),
                    It.IsAny<float>(),
                    It.IsAny<float>(),
                    It.IsAny<float>(),
                    It.IsAny<DateTime?>()
                 )
                )
                .ReturnsAsync(ServiceResult<Tuple<int, int>>.Success(Tuple.Create(1, 10)));

            // Arrange
            int refereeId = 1; // Filip Pavlík who has veto on team 1090181
            string matchId = "2023110A2B0404"; 
            int role = 1;
            bool force = true; // Force assignment despite veto
            string user = "test_user";

            // Act
            var result = await _controller.AddRefereeToTheMatch(refereeId, matchId, role, force, user);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var updatedMatch = await _adminRepo.GetMatchByIdAsync(matchId);
            var match = updatedMatch.GetDataOrThrow();
            Assert.Equal(refereeId, match.Ar1Id);
        }

        [Fact]
        public async Task RemoveRefereeFromTheMatch_WithValidData_ReturnsOkResult()
        {
            var mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients.All).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            // Arrange
            string matchId = "2023110A1A0102"; // Match with assigned referee
            int refereeId = 1; // Filip Pavlík assigned to this match
            string user = "test_user";

            // Act
            var result = await _controller.RemoveRefereeFromTheMatch(matchId, refereeId, user);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify referee was removed from match
            var updatedMatch = await _adminRepo.GetMatchByIdAsync(matchId);
            var match = updatedMatch.GetDataOrThrow();
            Assert.Null(match.RefereeId);
        }

        [Fact]
        public async Task RemoveRefereeFromTheMatch_WithNonExistentMatch_ReturnsServerError()
        {
            // Arrange
            string matchId = "NONEXISTENT";
            int refereeId = 1;
            string user = "test_user";

            // Act
            var result = await _controller.RemoveRefereeFromTheMatch(matchId, refereeId, user);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetCardInfo_WithValidRefereeId_ReturnsPartialView()
        {
            // Arrange
            int refereeId = 1; // Filip Pavlík from test data

            // Act
            var result = await _controller.GetCardInfo(refereeId);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/PartialViews/_RefereeCard.cshtml", partialViewResult.ViewName);
            Assert.NotNull(partialViewResult.Model);

            var model = Assert.IsType<RefereeCardViewModel>(partialViewResult.Model);
            Assert.NotNull(model.RefereeWTimeOptions);
            Assert.Equal("Filip", model.RefereeWTimeOptions.Referee.Name);
            Assert.Equal("Pavlík", model.RefereeWTimeOptions.Referee.Surname);
        }

        [Fact]
        public async Task GetCardInfo_WithNonExistentReferee_ReturnsServerError()
        {
            // Arrange
            int refereeId = 999; // Non-existent referee

            // Act
            var result = await _controller.GetCardInfo(refereeId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetExcuses_ReturnsPartialViewWithExcuses()
        {
            // Act
            var result = await _controller.GetExcuses();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/PartialViews/_ExcusesTable.cshtml", partialViewResult.ViewName);
            Assert.NotNull(partialViewResult.Model);

            var excuses = Assert.IsAssignableFrom<List<Excuse>>(partialViewResult.Model);
            Assert.Equal(3, excuses.Count); // Based on test data

            // Verify specific excuse data
            var firstExcuse = excuses.FirstOrDefault(e => e.RefereeId == 3);
            Assert.NotNull(firstExcuse);
            Assert.Equal("Rodinná svatba", firstExcuse.Reason);
        }

        [Fact]
        public async Task UpdateRefereeAsync_WithValidData_ReturnsOkResult()
        {
            this._emailsSender
            .Setup(sender => sender.AddEmailsToAllowedListAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Arrange
            int refereeId = 1; // Filip Pavlík
            string name = "Filip";
            string surname = "Pavlík";
            string idFacr = "02081713";
            string email = "updated@email.com";
            int age = 22;
            int league = 3;
            bool car = false;
            bool pfs = false;
            string place = "Praha 2";
            string note = "Updated note";

            // Act
            var result = await _controller.UpdateRefereeAsync(refereeId, name, surname, idFacr, email, age, league, car, pfs, place, note);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify referee was updated
            var updatedReferee = await _refereeRepo.GetRefereeByIdAsync(refereeId);
            var referee = updatedReferee.GetDataOrThrow();
            Assert.Equal("Filip", referee.Name);
            Assert.Equal("Pavlík", referee.Surname);
            Assert.Equal("updated@email.com", referee.Email);
            Assert.Equal(22, referee.Age);
            Assert.Equal(3, referee.League);
            Assert.False(referee.CarAvailability);
        }

        [Fact]
        public async Task UpdateRefereeAsync_WithNonExistentReferee_ReturnsServerError()
        {
            // Arrange
            int refereeId = 999; // Non-existent referee

            // Act
            var result = await _controller.UpdateRefereeAsync(refereeId, "Test", "Test", "TEST", "test@test.com", 25, 2, true, true, "Praha 1", "Test");

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task UploadRefreshedMatch_WithValidMatchId_ReturnsOkResult()
        {
            // Arrange
            string matchId = "2023110A1A0102"; // Existing match from test data

            // Act
            var result = await _controller.UploadRefreshedMatch(matchId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var matches = Assert.IsAssignableFrom<List<Models.Match>>(okResult.Value);
            Assert.True(matches.Count > 0);

            // Verify the specific match is in the result
            var targetMatch = matches.FirstOrDefault(m => m.MatchId == matchId);
            Assert.NotNull(targetMatch);
        }

        [Fact]
        public async Task AddRefereeToTheMatch_WithVetoAndNoForce_ReturnsBadRequest()
        {
            // Arrange
            int refereeId = 1; // Filip Pavlík who has veto on team 1090181
            string matchId = "2023110A2B0404"; // Match with team 1090181
            int role = 1;
            bool force = false; // Don't force, should respect veto
            string user = "test_user";

            // Act
            var result = await _controller.AddRefereeToTheMatch(refereeId, matchId, role, force, user);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, statusResult.StatusCode);
            Assert.Contains("veto", statusResult.Value.ToString().ToLower());
        }

        [Fact]
        public async Task AddRefereeToTheMatch_WithUnavailableTimeAndNoForce_ReturnsBadRequest()
        {
            // Arrange
            int refereeId = 3; // Karel Dvořák who has excuse during game time
            string matchId = "2023110A1A0102"; // Saturday match at 14:00
            int role = 1;
            bool force = false;
            string user = "test_user";

            // Act
            var result = await _controller.AddRefereeToTheMatch(refereeId, matchId, role, force, user);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, statusResult.StatusCode);
            Assert.Contains("nedostupný", statusResult.Value.ToString());
        }

        [Fact]
        public async Task GetCardInfo_ValidReferee_ContainsVetoes()
        {
            // Arrange
            int refereeId = 1; // Filip Pavlík who has vetoes

            // Act
            var result = await _controller.GetCardInfo(refereeId);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            var model = Assert.IsType<RefereeCardViewModel>(partialViewResult.Model);

            Assert.NotNull(model.Vetoes);
            Assert.True(model.Vetoes.Any());

            // Verify specific veto data
            var veto = model.Vetoes.FirstOrDefault(v => v.RefereeId == refereeId);
            Assert.NotNull(veto);
        }


        public void Dispose()
        {
            _adminContext.Dispose();
            _refereeContext.Dispose();
        }
    }
}