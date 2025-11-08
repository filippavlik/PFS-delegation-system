using AdminPartDevelop.Controllers;
using AdminPartDevelop.Data;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Hubs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Services.AdminServices;
using AdminPartDevelop.Services.FileParsers;
using AdminPartDevelop.Services.RefereeServices;
using AdminPartDevelop.Services.RouteServices;
using AdminPartDevelop.TestData;
using AdminPartDevelop.Views.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
    public class MatchControllerTest : IDisposable
    {
        private readonly AdminDbContext _adminContext;
        private readonly RefereeDbContext _refereeContext;
        private readonly MatchController _controller;
        private readonly AdminRepo _adminRepo;
        private readonly RefereeRepo _refereeRepo;
        private readonly IAdminService _adminService;
        private readonly IRefereeService _refereeService;
        private readonly Mock<IGeocodingService> _mockCar;
        private readonly Mock<IRouteBusPlanner> _mockBus;
        private readonly IExcelParser _excelParser;
        private readonly IExcelExporter _excelExporter;
        private readonly Mock<IHubContext<HubForReendering>> _mockHubContext;

        public MatchControllerTest()
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
            
            var mockControllerLogger = new Mock<ILogger<MatchController>>();
            var mockAdminServiceLogger = new Mock<ILogger<AdminService>>();
            var mockRefereeServiceLogger = new Mock<ILogger<RefereeService>>();
            var mockGetDataServiceLogger = new Mock<ILogger<GetData>>();
            var mockExportDataServiceLogger = new Mock<ILogger<ExportData>>();
            _mockCar = new Mock<IGeocodingService>();
            _mockBus = new Mock<IRouteBusPlanner>();



            _adminService = new AdminService(_adminRepo,mockAdminServiceLogger.Object);
            _refereeService = new RefereeService( _adminRepo,_mockCar.Object,_mockBus.Object, mockRefereeServiceLogger.Object);
            _excelParser = new GetData(mockGetDataServiceLogger.Object);
            _excelExporter = new ExportData(mockExportDataServiceLogger.Object);
            _mockHubContext = new Mock<IHubContext<HubForReendering>>();



            // Create controller
            _controller = new MatchController(
                _refereeRepo,
                _adminRepo,
                _excelParser,
                _excelExporter,
                _refereeService,
                _adminService,
                _mockHubContext.Object,
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
        public async Task GetMatchesByDate_WithValidDateRange_ReturnsPartialView()
        {
            // Arrange
            var gameDate = DateOnly.FromDateTime(DateTime.Now.AddDays(6));
            var startDate = gameDate;
            var startTime = new TimeOnly(0, 0);
            var endDate = gameDate.AddDays(1);
            var endTime = new TimeOnly(23, 59);

            // Act
            var result = await _controller.GetMatchesByDate(startDate, startTime, endDate, endTime);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/PartialViews/_MatchesTable.cshtml", partialViewResult.ViewName);
            Assert.NotNull(partialViewResult.Model);
            var model = Assert.IsAssignableFrom<List<MatchViewModel>>(partialViewResult.Model);
            Assert.Equal(2, model.Count);
        }
        [Fact]
        public async Task GetMatchesByDate_WithValidDateRange_ReturnsNoMatchesInPartialView()
        {
            // Arrange
            var gameDate = DateOnly.FromDateTime(DateTime.Now.AddDays(8));
            var startDate = gameDate;
            var startTime = new TimeOnly(0, 0);
            var endDate = gameDate.AddDays(1);
            var endTime = new TimeOnly(23, 59);

            // Act
            var result = await _controller.GetMatchesByDate(startDate, startTime, endDate, endTime);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/PartialViews/_MatchesTable.cshtml", partialViewResult.ViewName);
            Assert.NotNull(partialViewResult.Model);
            var model = Assert.IsAssignableFrom<List<MatchViewModel>>(partialViewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task GetRefereeMatchCounts_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new RefereeMatchCountViewModel
            {
                RefereeIds = new List<int> { 1, 2 },
                TeamId = "1040231",
                IsReferee = true,
                CompetitionId = "2023110A1A"
            };

            // Act
            var result = await _controller.GetRefereeMatchCounts(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<List<RefereesTeamsMatchesResponseDto>>(okResult.Value);

            Assert.Equal(2, actual.Count);

            Assert.Collection(actual,
                item =>
                {
                    Assert.Equal(1, item.RefereeId);
                    Assert.Equal(1, item.HomeCount);
                    Assert.Equal(0, item.AwayCount);
                },
                item =>
                {
                    Assert.Equal(2, item.RefereeId);
                    Assert.Equal(0, item.HomeCount);
                    Assert.Equal(0, item.AwayCount);
                });


            // Arrange
            var requestAr = new RefereeMatchCountViewModel
            {
                RefereeIds = new List<int> { 1, 2 },
                TeamId = "1040231",
                IsReferee = false,
                CompetitionId = "2023110A1A"
            };

            // Act
            var resultAr = await _controller.GetRefereeMatchCounts(requestAr);

            // Assert
            var okResultAr = Assert.IsType<OkObjectResult>(resultAr);
            var actualAr = Assert.IsAssignableFrom<List<RefereesTeamsMatchesResponseDto>>(okResultAr.Value);

            Assert.Equal(2, actualAr.Count);

            Assert.Collection(actualAr,
                item =>
                {
                    Assert.Equal(1, item.RefereeId);
                    Assert.Equal(0, item.HomeCount);
                    Assert.Equal(1, item.AwayCount);
                },
                item =>
                {
                    Assert.Equal(2, item.RefereeId);
                    Assert.Equal(0, item.HomeCount);
                    Assert.Equal(1, item.AwayCount);
                });
        }

        [Fact]
        public async Task GetSortedMatchesAsync_WithValidSelector_ReturnsPartialView()
        {
            // Arrange
            string selector = "sortByGameTimeAsc";

            // Act
            var result = await _controller.GetSortedMatchesAsync(selector);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/PartialViews/_MatchesTable.cshtml", partialViewResult.ViewName);
            Assert.NotNull(partialViewResult.Model);
            var model = Assert.IsAssignableFrom<List<MatchViewModel>>(partialViewResult.Model);
            var matchList = model.ToList();

            Assert.Equal(5, matchList.Count);

            for (int i = 0; i < matchList.Count - 1; i++)
            {
                Assert.True(matchList[i].Match.MatchDate.ToDateTime(matchList[i].Match.MatchTime) <= 
                    matchList[i+1].Match.MatchDate.ToDateTime(matchList[i+1].Match.MatchTime), $"Match at index {i} has later GameTime than the next one.");
            }

        }

        [Fact]
        public async Task GetRefereesPoints_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new RefereeMatchPointsViewModel
            {
                RefereeIds = new List<int> { 1, 2 },
                MatchId = "2023110A1A0102",
                HomeTeamId = "1040231",
                AwayTeamId = "10A0151",
                IsReferee = true
            };

            var calculatedPoints = new Dictionary<int,Tuple<int,string>>
            {
                    { 1, Tuple.Create(85, "Good performance") },
                    { 2, Tuple.Create(72, "Average performance") }
            };

            // Act
            var result = await _controller.GetRefereesPoints(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(calculatedPoints.GetValueOrDefault(1), Tuple.Create(85, "Good performance") );
            Assert.Equal(calculatedPoints.GetValueOrDefault(2), Tuple.Create(72, "Average performance"));

        }

        [Fact]
        public async Task GetRefereeMatches_WithValidRefereeList_ReturnsOkResult()
        {
            // Arrange
            var refereeList = new List<Referee>
            {
                new Referee { RefereeId = 1 },
                new Referee { RefereeId = 2 }
            };

            // Act
            var result = await _controller.GetRefereeMatches(refereeList);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultData = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.NotNull(resultData);
            var resultList = resultData
                .Select(x => new
                {
                    RefereeId = (int)x.GetType().GetProperty("RefereeId")!.GetValue(x)!,
                    AsMainReferee = (int)x.GetType().GetProperty("AsMainReferee")!.GetValue(x)!,
                    AsAssistantReferee = (int)x.GetType().GetProperty("AsAssistantReferee")!.GetValue(x)!
                })
                .ToList();

            // Assert expected count
            Assert.Equal(2, resultList.Count);

            // Assert specific values
            Assert.Contains(resultList, r => r.RefereeId == 1 && r.AsMainReferee == 1 && r.AsAssistantReferee == 2);
            Assert.Contains(resultList, r => r.RefereeId == 2 && r.AsMainReferee == 0 && r.AsAssistantReferee == 1);
        }

        [Fact]
        public async Task GetTeamsByInput_WithValidInput_ReturnsOkResult()
        {
            // Arrange
            string input = "Praha";

            // Act
            var result = await _controller.GetTeamsByInput(input);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var model = Assert.IsAssignableFrom<List<Models.Team>>(okResult.Value);
            Assert.Equal(4, model.Count());

        }
        [Fact]
        public async Task GetTeamsByInput_WithInValidInput_ReturnsOkResult()
        {
            // Arrange
            string input = "xyZ";

            // Act
            var result = await _controller.GetTeamsByInput(input);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var model = Assert.IsAssignableFrom<List<Models.Team>>(okResult.Value);
            Assert.Empty(model);

        }

        [Fact]
        public async Task GetCompetitions_ReturnsOkResult()
        {
            // Act
            var result = await _controller.GetCompetitions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var competitions = okResult.Value as List<Competition>;
            Assert.NotNull(competitions);
            Assert.True(competitions.Count == 6);
        }

        [Fact]
        public async Task GetPreviousMatches_ReturnsPartialView()
        {
            // Act
            var result = await _controller.GetPreviousMatches();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/PartialViews/_PreviousMatchesTable.cshtml", partialViewResult.ViewName);
            var previousMatches = partialViewResult.Model as List<FilesPreviousDelegation>;

            Assert.NotNull(previousMatches);
            Assert.True(previousMatches.Count == 2);

        }

        [Fact]
        public async Task LockOrUnlockMatch_WithValidMatchId_Lock_ReturnsOkResult()
        {
            // Arrange
            string matchId = "2023110A1A0102";
            string user = "testuser";

            var mockClients = new Mock<IHubCallerClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients.All).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.LockOrUnlockMatch(matchId, user);

            // Assert
            Assert.IsType<OkResult>(result);

            var match = await _adminContext.Matches.Where(m => m.MatchId ==matchId).FirstOrDefaultAsync();
            Assert.NotNull(match);
            Assert.True(match.Locked);
        }
        [Fact]
        public async Task LockOrUnlockMatch_WithValidMatchId_Unlock_ReturnsOkResult()
        {
            // Arrange
            string matchId = "2023110A1A0110";
            string user = "testuser";

            var mockClients = new Mock<IHubCallerClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients.All).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.LockOrUnlockMatch(matchId, user);

            // Assert
            Assert.IsType<OkResult>(result);

            var match = await _adminContext.Matches.Where(m => m.MatchId == matchId).FirstOrDefaultAsync();
            Assert.NotNull(match);
            Assert.False(match.Locked);
        }

        [Fact]
        public async Task MakeMatchPlayed_WithValidMatchId_ReturnsOkResult()
        {
            // Arrange
            string matchId = "2023110A2B0404";
            string user = "testuser";

            // Act
            var result = await _controller.MakeMatchPlayed(matchId, user);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Zápas úspěšně změněn na odehraný!", okResult.Value);

            var match = await _adminContext.Matches.Where(m => m.MatchId == matchId).FirstOrDefaultAsync();
            Assert.NotNull(match);
            Assert.True(match.AlreadyPlayed);
        }

        [Fact]
        public async Task UploadMatchesFromFileAsync_WithValidFile_ReturnsBadResult()
        {
            // Arrange
            var content = "Test file content";
            var fileName = "test.xlsx";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName);
            string user = "testuser";

            // Act
            var result = await _controller.UploadMatchesFromFileAsync(formFile, user);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, okResult.StatusCode);
        }

        [Fact]
        public async Task MakeConnectionsOfMatches_ReturnsPartialView()
        {
            // Act
            var result = await _controller.MakeConnectionsOfMatches();
            
            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/PartialViews/_MatchesTable.cshtml", partialViewResult.ViewName);
            Assert.NotNull(partialViewResult.Model);
            var connectedMatches = Assert.IsAssignableFrom<List<MatchViewModel>>(partialViewResult.Model);
            Assert.Null(connectedMatches[3].Match.PreMatch);
            Assert.Equal("2023110A1A0110", connectedMatches[3].Match.PostMatch);
            Assert.Equal("2023002E3C0503", connectedMatches[4].Match.PreMatch);
            Assert.Null(connectedMatches[4].Match.PostMatch);
            Assert.Null(connectedMatches[2].Match.PreMatch);
            Assert.Null(connectedMatches[2].Match.PostMatch);
        }

        [Fact]
        public async Task DownloadFileWithFilledMatches_ReturnsFileResult()
        {
            // Act
            var result = await _controller.DownloadFileWithFilledMatches();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
            Assert.Contains("matches_", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetRefereeMatchCounts_WithInvalidTeamAndCompetition_ReturnsZeroMatches()
        {
            // Arrange
            var request = new RefereeMatchCountViewModel
            {
                RefereeIds = new List<int> { 1 ,2},
                TeamId = "invalid",
                IsReferee = true,
                CompetitionId = "invalid"
            };

            // Act
            var result = await _controller.GetRefereeMatchCounts(request);

            // Assert
            var statusCodeResult = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<List<RefereesTeamsMatchesResponseDto>>(statusCodeResult.Value);

            Assert.Equal(200, statusCodeResult.StatusCode);
            Assert.Equal(2, actual.Count);

            Assert.Collection(actual,
                item =>
                {
                    Assert.Equal(1, item.RefereeId);
                    Assert.Equal(0, item.HomeCount);
                    Assert.Equal(0, item.AwayCount);
                },
                item =>
                {
                    Assert.Equal(2, item.RefereeId);
                    Assert.Equal(0, item.HomeCount);
                    Assert.Equal(0, item.AwayCount);
                });
            
        }

        public void Dispose()
        {
            _adminContext.Dispose();
            _refereeContext.Dispose();
        }
    }
}