using AdminPartDevelop.Services.RefereeServices;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AdminPartDevelop.Models;
using AdminPartDevelop.Views.ViewModels;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Services.RouteServices;

namespace AdminPartDevelop.Tests.Services.RefereeServices
{
    public class RefereeServiceTests
    {
        private readonly Mock<ILogger<RefereeService>> _loggerMock;
        private readonly Mock<Data.IAdminRepo> _AdminRepoMock;
        private readonly RefereeService _refereeService;
        private readonly Mock<IRouteCarPlanner> _mockCar;
        private readonly Mock<IRouteBusPlanner> _mockBus;

        public RefereeServiceTests()
        {
            _loggerMock = new Mock<ILogger<RefereeService>>();
            _AdminRepoMock = new Mock<Data.IAdminRepo>();
            _mockCar = new Mock<IRouteCarPlanner>();
            _mockBus = new Mock<IRouteBusPlanner>();

            _refereeService = new RefereeService(_AdminRepoMock.Object, _mockCar.Object, _mockBus.Object, _loggerMock.Object);
        }

        [Fact]
        public void SortRefereesByLeague_WhenValid_ShouldReturnRefereesSortedByLeague()
        {
            // Arrange
            var referees = new List<RefereeWithTimeOptions>
            {
                new RefereeWithTimeOptions(new Referee { RefereeId = 1, Name = "John", Surname = "Smith", League = 0 }),
                new RefereeWithTimeOptions(new Referee { RefereeId = 2, Name = "Jane", Surname = "Doe", League = 1 }),
                new RefereeWithTimeOptions(new Referee { RefereeId = 3, Name = "Bob", Surname = "Brown", League = 2 }),
                new RefereeWithTimeOptions(new Referee { RefereeId = 4, Name = "Alice", Surname = "Brown", League = 2 })
            };

            // Act
            var result = _refereeService.SortRefereesByLeague(referees);

            // Assert
            Assert.True(result.IsSuccess);
            var sortedReferees = result.GetDataOrThrow();
            Assert.Equal(6, sortedReferees.Count);
            Assert.Single(sortedReferees[0]); 
            Assert.Single(sortedReferees[1]); 
            Assert.Equal(2, sortedReferees[2].Count); 
            Assert.Equal("Brown", sortedReferees[2][0].Referee.Surname);
            Assert.Equal("Brown", sortedReferees[2][1].Referee.Surname);
            Assert.Equal("Alice", sortedReferees[2][0].Referee.Name); 
            Assert.Equal("Bob", sortedReferees[2][1].Referee.Name);
        }

        [Fact]
        public void SortRefereesByLeague_WithInvalidLeague_ShouldReturnFailure()
        {
            // Arrange
            var referees = new List<RefereeWithTimeOptions>
            {
                new RefereeWithTimeOptions(new Referee { RefereeId = 1, Name = "John", Surname = "Smith", League = 0 }),
                new RefereeWithTimeOptions(new Referee { RefereeId = 2, Name = "Jane", Surname = "Doe", League = 6 }) // Invalid league
            };

            // Act
            var result = _refereeService.SortRefereesByLeague(referees);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("nemá přiradzenou ligu", result.ErrorMessage);
        }

        [Fact]
        public async Task AddRefereeTimeOptionsAsync_ValidReferee_ReturnsTimeOptions()
        {
            // Arrange
            var referee = new Referee
            {
                RefereeId = 1,
                Name = "John",
                Surname = "Smith",
                League = 1,
                Excuses = new List<Excuse>
                {
                    new Excuse
                    {
                        ExcuseId = 1,
                        DateFrom = new DateOnly(2025, 5, 17),
                        TimeFrom = new TimeOnly(9, 0),
                        DateTo = new DateOnly(2025, 5, 17),
                        TimeTo = new TimeOnly(12, 0),
                        Note = "Morning appointment"
                    },
                    new Excuse
                    {
                        ExcuseId = 2,
                        DateFrom = new DateOnly(2025, 5, 17),
                        TimeFrom = new TimeOnly(15, 0),
                        DateTo = new DateOnly(2025, 5, 17),
                        TimeTo = new TimeOnly(18, 0),
                        Note = "Noon appointment"
                    }
                },
                VehicleSlots = new List<VehicleSlot>
                {
                    new VehicleSlot
                    {
                        SlotId = 1,
                        DateFrom = new DateOnly(2025, 5, 17),
                        TimeFrom = new TimeOnly(14, 0),
                        DateTo = new DateOnly(2025, 5, 17),
                        TimeTo = new TimeOnly(18, 0),
                        HasCarInTheSlot = true
                    },
                    new VehicleSlot
                    {
                        SlotId = 2,
                        DateFrom = new DateOnly(2025, 5, 18),
                        TimeFrom = new TimeOnly(14, 0),
                        DateTo = new DateOnly(2025, 5, 18),
                        TimeTo = new TimeOnly(18, 0),
                        HasCarInTheSlot = false
                    }

                }
            };

            var matches = new List<Models.Match>
            {
                new Models.Match
                {
                    MatchId = "M1",
                    RefereeId = 1,
                    MatchDate = new DateOnly(2025, 5, 17),
                    MatchTime = new TimeOnly(15, 0),
                    Competition = new Competition { MatchLength = 45 }
                },
                 new Models.Match
                {
                    MatchId = "M2",
                    RefereeId = 2,
                    Ar1Id = 4,
                    Ar2Id = 3,
                    MatchDate = new DateOnly(2025, 5, 19),
                    MatchTime = new TimeOnly(15, 0),
                    Competition = new Competition { MatchLength = 45 }
                },
                 new Models.Match
                {
                    MatchId = "M3",
                    RefereeId = 2,
                    Ar1Id = 1,
                    Ar2Id = 4,
                    MatchDate = new DateOnly(2025, 5, 18),
                    MatchTime = new TimeOnly(15, 0),
                    Competition = new Competition { MatchLength = 35 }
                },
                new Models.Match
                {
                    MatchId = "M4",                
                    MatchDate = new DateOnly(2025, 5, 19),
                    MatchTime = new TimeOnly(15, 0),
                    Competition = new Competition { MatchLength = 30}
                },
            };

            var transfers = new List<Transfer>
            {
                new Transfer
                {
                    TransferId = 1,
                    RefereeId = 1,
                    ExpectedDeparture = new DateTime(2025, 5, 17, 18, 30, 0),
                    ExpectedArrival = new DateTime(2025, 5, 17, 19, 30, 0)
                },
                new Transfer
                {
                    TransferId = 1,
                    RefereeId = 2,
                    ExpectedDeparture = new DateTime(2025, 5, 17, 18, 30, 0),
                    ExpectedArrival = new DateTime(2025, 5, 17, 19, 30, 0)
                }
            };

            var firstGameDay = new DateOnly(2025, 5, 17);

            // Act
            var result = (await _refereeService.AddRefereeTimeOptionsAsync(referee, matches, transfers, firstGameDay));

            // Assert
            Assert.True(result.IsSuccess);

            var timeOptions = result.GetDataOrThrow();
            Assert.Equal(referee, timeOptions.Referee);
            Assert.True(timeOptions.hasSpecialNote);

            // Verify all time ranges are included (excuse, match, vehicle slot, transfer)
            Assert.Equal(7, timeOptions.SortedRanges.Count);

            // Check specific time ranges
            var excuseRanges = timeOptions.SortedRanges.Where(r => r.RangeType == "omluva").ToList();

            Assert.NotNull(excuseRanges[0]);
            Assert.Equal(1, excuseRanges[0].ExcuseId);
            Assert.NotNull(excuseRanges[1]);
            Assert.Equal(2, excuseRanges[1].ExcuseId);

            var matchRange = timeOptions.SortedRanges.FirstOrDefault(r => r.RangeType == "zapasref");
            Assert.NotNull(matchRange);
            Assert.Equal("M1", matchRange.MatchId);
            var matchRangeAr = timeOptions.SortedRanges.FirstOrDefault(r => r.RangeType == "zapasar");
            Assert.NotNull(matchRangeAr);
            Assert.Equal("M3", matchRangeAr.MatchId);

            var vehicleRanges = timeOptions.SortedRanges.Where(r => r.RangeType == "vozidlo").ToList();
            Assert.NotNull(vehicleRanges[0]);
            Assert.Equal(1, vehicleRanges[0].SlotId);
            Assert.NotNull(vehicleRanges[1]);
            Assert.Equal(2, vehicleRanges[1].SlotId);

            var transferRanges = timeOptions.SortedRanges.Where(r => r.RangeType == "transfer").ToList();
            Assert.NotNull(transferRanges[0]);
            Assert.Equal(1, transferRanges[0].TransferId);
        }

        [Fact]
        public async Task AddRefereesTimeOptionsAsync_MultipleReferees_ReturnsAllTimeOptions()
        {
            // Arrange
            var referees = new List<Referee>
            {
                new Referee
                {
                    RefereeId = 1,
                    Name = "John",
                    Surname = "Smith",
                    League = 1,
                    Excuses = new List<Excuse>
                    {
                        new Excuse
                        {
                            ExcuseId = 1,
                            DateFrom = new DateOnly(2025, 5, 17),
                            TimeFrom = new TimeOnly(9, 0),
                            DateTo = new DateOnly(2025, 5, 17),
                            TimeTo = new TimeOnly(12, 0),
                            Note = "Morning appointment"
                        }
                    },
                    VehicleSlots = new List<VehicleSlot>()
                },
                new Referee
                {
                    RefereeId = 2,
                    Name = "Jane",
                    Surname = "Doe",
                    League = 2,
                    Excuses = new List<Excuse>
                    {
                     new Excuse
                        {
                            ExcuseId = 2,
                            DateFrom = new DateOnly(2025, 5, 16),
                            TimeFrom = new TimeOnly(9, 0),
                            DateTo = new DateOnly(2025, 5, 17),
                            TimeTo = new TimeOnly(10, 0),
                        },
                    },
                    VehicleSlots = new List<VehicleSlot>
                    {                        
                        new VehicleSlot
                        {
                            SlotId = 2,
                            DateFrom = new DateOnly(2025, 5, 17),
                            TimeFrom = new TimeOnly(14, 0),
                            DateTo = new DateOnly(2025, 5, 17),
                            TimeTo = new TimeOnly(18, 0),
                            HasCarInTheSlot = true
                        }
                    }
                }
            };

            var matchViewModels = new List<MatchViewModel>
            {
                new MatchViewModel
                {
                   Match = new AdminPartDevelop.Models.Match
                    {
                        MatchId = "2023110A1A0106",
                        HomeTeamId = "10A0141",
                        AwayTeamId = "1080021",
                        MatchDate = new DateOnly(2025, 5, 17),
                        MatchTime = new TimeOnly(14, 0),
                        AlreadyPlayed = false,
                        Locked = false,
                        LastChangedBy = "Admin",
                        LastChanged = new DateTime(2025, 1, 1, 10, 0, 0),
                        Competition = new Competition
                        {
                            MatchLength = 45
                        }
                    },
                    HomeTeamName = "TJ Sokol Královice, z.s.",
                    AwayTeamName = "Sportovní klub Dolní Chabry, z.s.",
                    FieldName = "KRÁLOVICE  T.",
                    CompetitionName = "League A"
                },
                new MatchViewModel
                {
                    Match = new AdminPartDevelop.Models.Match
                    {
                        MatchId = "2023110A3A0108",
                        HomeTeamId = "1050081",
                        AwayTeamId = "1050171",
                        MatchDate = new DateOnly(2025, 5, 18),
                        MatchTime = new TimeOnly(16, 00),
                        RefereeId = 1,
                        Ar2Id = 2,
                        AlreadyPlayed = true,
                        Locked = true,
                        LastChangedBy = "User",
                        LastChanged = new DateTime(2025, 1, 1, 12, 0, 0),
                        Competition = new Competition
                        {
                            MatchLength = 45
                        }
                    },
                    HomeTeamName = "SC Olympia Radotín z.s.",
                    AwayTeamName = " FOTBALOVÝ KLUB FC ZLIČÍN",
                    FieldName = "SC RADOTÍN  UMT.",
                    CompetitionName = "League B"
                }
            };

            var transfers = new List<Transfer>();
            var firstGameDay = new DateOnly(2025, 5, 17);

            // Act
            var result = await _refereeService.AddRefereesTimeOptionsAsync(referees, matchViewModels, transfers, firstGameDay);

            // Assert
            var timeOptionsList = result.GetDataOrThrow();
            Assert.Equal(2, timeOptionsList.Count);           

            // First referee should have an excuse and a match as main referee
            var referee1Options = timeOptionsList.First(r => r.Referee.RefereeId == 1);           
            Assert.Contains(referee1Options.SortedRanges, r => r.RangeType == "omluva" && r.ExcuseId==1);
            Assert.Contains(referee1Options.SortedRanges, r => (r.RangeType == "zapasref" && r.MatchId== "2023110A3A0108"));
            Assert.False(referee1Options.isFreeSaturdayMorning);
            Assert.False(referee1Options.isFreeSundayAfternoon);
            Assert.True(referee1Options.isFreeSundayMorning);
            Assert.True(referee1Options.isFreeSaturdayAfternoon);
            Assert.True(referee1Options.hasSpecialNote);

            // Second referee should have a vehicle slot and a match as assistant
            var referee2Options = timeOptionsList.First(r => r.Referee.RefereeId == 2);
            Assert.Contains(referee2Options.SortedRanges, r => r.RangeType == "omluva" && r.ExcuseId == 2);
            Assert.Contains(referee2Options.SortedRanges, r => r.RangeType == "vozidlo" && r.SlotId==2);
            Assert.Contains(referee2Options.SortedRanges, r => r.RangeType == "zapasar" && r.MatchId == "2023110A3A0108");
            Assert.False(referee2Options.hasSpecialNote);
            Assert.True(referee2Options.isFreeSaturdayAfternoon);
            Assert.False(referee2Options.isFreeSaturdayMorning);
            Assert.False(referee2Options.isFreeSundayAfternoon);
            Assert.True(referee2Options.isFreeSundayMorning);
            Assert.False(referee2Options.hasSpecialNote);

        }

        [Fact]
        public async Task AddRefereesTimeOptionsAsync_WholeWeekendExcuse_ReturnsAllTimeOptions()
        {
            // Arrange
            var referees = new List<Referee>
            {
                new Referee
                {
                    RefereeId = 1,
                    Name = "John",
                    Surname = "Smith",
                    League = 1,
                    Excuses = new List<Excuse>
                    {
                        new Excuse
                        {
                            ExcuseId = 1,
                            DateFrom = new DateOnly(2025, 5, 15),
                            TimeFrom = new TimeOnly(9, 0),
                            DateTo = new DateOnly(2025, 5, 19),
                            TimeTo = new TimeOnly(12, 0),
                            Note = "Morning appointment"
                        }
                    },
                    VehicleSlots = new List<VehicleSlot>()
                },
                new Referee
                {
                    RefereeId = 2,
                    Name = "Jane",
                    Surname = "Doe",
                    League = 2,
                    Excuses = new List<Excuse>
                    {                  
                    },
                    VehicleSlots = new List<VehicleSlot>
                    {
                        new VehicleSlot
                        {
                            SlotId = 2,
                            DateFrom = new DateOnly(2025, 5, 17),
                            TimeFrom = new TimeOnly(14, 0),
                            DateTo = new DateOnly(2025, 5, 17),
                            TimeTo = new TimeOnly(18, 0),
                            HasCarInTheSlot = true
                        }
                    }
                }
            };

            var matchViewModels = new List<MatchViewModel>
            {
                new MatchViewModel
                {
                   Match = new AdminPartDevelop.Models.Match
                    {
                        MatchId = "2023110A1A0106",
                        HomeTeamId = "10A0141",
                        AwayTeamId = "1080021",
                        MatchDate = new DateOnly(2025, 5, 17),
                        MatchTime = new TimeOnly(14, 0),
                        RefereeId = 2,
                        AlreadyPlayed = false,
                        Locked = false,
                        LastChangedBy = "Admin",
                        LastChanged = new DateTime(2025, 1, 1, 10, 0, 0),
                         Competition = new Competition
                        {
                            MatchLength = 45
                        }
                    },
                    HomeTeamName = "TJ Sokol Královice, z.s.",
                    AwayTeamName = "Sportovní klub Dolní Chabry, z.s.",
                    FieldName = "KRÁLOVICE  T.",
                    CompetitionName = "League A"
                },
                new MatchViewModel
                {
                    Match = new AdminPartDevelop.Models.Match
                    {
                        MatchId = "2023110A3A0108",
                        HomeTeamId = "1050081",
                        AwayTeamId = "1050171",
                        MatchDate = new DateOnly(2025, 5, 18),
                        MatchTime = new TimeOnly(16, 00),
                        RefereeId = 1,
                        Ar2Id = 2,
                        AlreadyPlayed = false,
                        Locked = true,
                        LastChangedBy = "User",
                        LastChanged = new DateTime(2025, 1, 1, 12, 0, 0),
                        Competition = new Competition
                        {
                            MatchLength = 45
                        }
                    },
                    HomeTeamName = "SC Olympia Radotín z.s.",
                    AwayTeamName = " FOTBALOVÝ KLUB FC ZLIČÍN",
                    FieldName = "SC RADOTÍN  UMT.",
                    CompetitionName = "League B"
                }
            };

            var transfers = new List<Transfer>();
            var firstGameDay = new DateOnly(2025, 5, 17);

            // Act
            var result = await _refereeService.AddRefereesTimeOptionsAsync(referees, matchViewModels, transfers, firstGameDay);

            // Assert
            var timeOptionsList = result.GetDataOrThrow();
            Assert.Equal(2, timeOptionsList.Count);

            // First referee should have an excuse and a match as main referee
            var referee1Options = timeOptionsList.First(r => r.Referee.RefereeId == 1);
            Assert.Contains(referee1Options.SortedRanges.Where(r => r.RangeType == "omluva"), r => r.ExcuseId == 1);
            Assert.Contains(referee1Options.SortedRanges.Where(r => r.RangeType == "zapasref"), r => r.MatchId == "2023110A3A0108");

            Assert.False(referee1Options.isFreeSaturdayMorning);
            Assert.False(referee1Options.isFreeSundayAfternoon);
            Assert.False(referee1Options.isFreeSundayMorning);
            Assert.False(referee1Options.isFreeSaturdayAfternoon);

            // Second referee should have a vehicle slot and a match as assistant
            var referee2Options = timeOptionsList.First(r => r.Referee.RefereeId == 2);
            Assert.Contains(referee2Options.SortedRanges, r => r.RangeType == "vozidlo" && r.SlotId == 2);
            Assert.Contains(referee2Options.SortedRanges, r => r.RangeType == "zapasref" && r.MatchId == "2023110A1A0106");
            Assert.Contains(referee2Options.SortedRanges, r => r.RangeType == "zapasar" && r.MatchId == "2023110A3A0108");
            Assert.False(referee2Options.hasSpecialNote);
            Assert.False(referee2Options.isFreeSaturdayAfternoon);
            Assert.True(referee2Options.isFreeSaturdayMorning);
            Assert.False(referee2Options.isFreeSundayAfternoon);
            Assert.True(referee2Options.isFreeSundayMorning);
            Assert.False(referee2Options.hasSpecialNote);




        }

        [Fact]
        public void GetFirstNextMatchDateTime_HasNextMatch_ReturnsMatch()
        {
            // Arrange
            var referee = new RefereeWithTimeOptions(new Referee { RefereeId = 1 });
            referee.SortedRanges.Add(new TimeRange(
                new DateTime(2025, 5, 17, 14, 0, 0),
                new DateTime(2025, 5, 17, 16, 0, 0),
                "zapasref",
                matchId: "M1"
            ));
            referee.SortedRanges.Add(new TimeRange(
                new DateTime(2025, 5, 17, 18, 0, 0),
                new DateTime(2025, 5, 17, 20, 0, 0),
                "zapasref",
                matchId: "M2"
            ));

            var currentMatch = new Models.Match
            {
                MatchId = "M0",
                MatchDate = new DateOnly(2025, 5, 17),
                MatchTime = new TimeOnly(12, 0),
                Competition = new Competition { MatchLength = 45 }
            };

            // Act
            var result = _refereeService.GetFirstNextMatchDateTime(referee, currentMatch);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GetDataOrThrow());
            Assert.Equal(new DateTime(2025, 5, 17, 14, 0, 0), result.GetDataOrThrow().Item1);
            Assert.Equal("M1", result.GetDataOrThrow().Item2);
        }

        [Fact]
        public void GetFirstNextMatchDateTime_NoMatchWithinTimeframe_ReturnsNull()
        {
            // Arrange
            var referee = new RefereeWithTimeOptions(new Referee { RefereeId = 1 });
            referee.SortedRanges.Add(new TimeRange(
                new DateTime(2025, 5, 17, 18, 0, 0), // More than 90 minutes after current match ends
                new DateTime(2025, 5, 17, 20, 0, 0),
                "zapasref",
                matchId: "M2"
            ));

            var currentMatch = new Models.Match
            {
                MatchId = "M0",
                MatchDate = new DateOnly(2025, 5, 17),
                MatchTime = new TimeOnly(12, 0),
                Competition = new Competition { MatchLength = 45 }
            };

            // Act
            var result = _refereeService.GetFirstNextMatchDateTime(referee, currentMatch);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.GetDataOrThrow());
        }

        [Fact]
        public void CheckTimeAvailabilityOfReferee_RefereeAvailable_ReturnsTrue()
        {
            // Arrange
            var referee = new RefereeWithTimeOptions(new Referee { RefereeId = 1 });
            referee.SortedRanges.Add(new TimeRange(
                new DateTime(2025, 5, 17, 10, 0, 0),
                new DateTime(2025, 5, 17, 12, 0, 0),
                "zapasref"
            ));
            referee.SortedRanges.Add(new TimeRange(
                new DateTime(2025, 5, 17, 17, 30, 0),
                new DateTime(2025, 5, 17, 20, 0, 0),
                "omluva"
            ));

            var match = new Models.Match
            {
                MatchId = "M1",
                MatchDate = new DateOnly(2025, 5, 17),
                MatchTime = new TimeOnly(14, 0), // Between the two existing ranges
                Competition = new Competition { MatchLength = 45 }
            };

            // Act
            var result = _refereeService.CheckTimeAvailabilityOfReferee(referee, match);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.GetDataOrThrow());
        }

        [Fact]
        public void CheckTimeAvailabilityOfReferee_RefereeNotAvailable_ReturnsFalse()
        {
            // Arrange
            var referee = new RefereeWithTimeOptions(new Referee { RefereeId = 1 });
            referee.SortedRanges.Add(new TimeRange(
                new DateTime(2025, 5, 17, 13, 0, 0),
                new DateTime(2025, 5, 17, 15, 0, 0),
                "zapasref"
            ));

            var match = new Models.Match
            {
                MatchId = "M1",
                MatchDate = new DateOnly(2025, 5, 17),
                MatchTime = new TimeOnly(14, 0), // Overlaps with existing range
                Competition = new Competition { MatchLength = 45 }
            };

            // Act
            var result = _refereeService.CheckTimeAvailabilityOfReferee(referee, match);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.GetDataOrThrow());
        }

        [Fact]
        public void CheckCarAvailabilityOfReferee_HasCar_ReturnsTrue()
        {
            // Arrange
            var referee = new RefereeWithTimeOptions(
                new Referee
                {
                    RefereeId = 1,
                    VehicleSlots = new List<VehicleSlot>
                    {
                        new VehicleSlot
                        {
                            SlotId = 1,
                            HasCarInTheSlot = true
                        }
                    }
                }
            );

            referee.SortedRanges.Add(new TimeRange(
                new DateTime(2025, 5, 17, 13, 0, 0),
                new DateTime(2025, 5, 17, 17, 0, 0),
                "vozidlo",
                slotId: 1
            ));

            var match = new Models.Match
            {
                MatchId = "M1",
                MatchDate = new DateOnly(2025, 5, 17),
                MatchTime = new TimeOnly(14, 0), // During vehicle slot
                Competition = new Competition { MatchLength = 45 }
            };

            // Act
            var result = _refereeService.CheckCarAvailabilityOfReferee(referee, match);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.GetDataOrThrow());
            Assert.True(result.GetDataOrThrow());
        }

        [Fact]
        public void CheckTimeAvailabilityWithTransferOfReferee_PreMatchTransferPossible_ReturnsTrue()       
        {
            // Arrange
            var endOfPreviousMatch = new DateTime(2025, 5, 17, 13, 0, 0);
            var connectedMatchId = "M1";
            var transferLength = 30; // 30 minutes
            var refereeId = 1;
            var hasCar = true;

            var match = new Models.Match
            {
                MatchId = "M2",
                MatchDate = new DateOnly(2025, 5, 17),
                MatchTime = new TimeOnly(15, 0), // 1 hour after previous match ends
                Competition = new Competition { MatchLength = 45 }
            };

            // Act
            var result = _refereeService.CheckTimeAvailabilityWithTransferOfReferee(
                endOfPreviousMatch,
                connectedMatchId,
                match,
                transferLength,
                refereeId,
                true, // is pre-match 
                hasCar
            );

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.GetDataOrThrow().Item1); // Transfer is possible
            var transfer = result.GetDataOrThrow().Item3;
            Assert.Equal(refereeId, transfer.RefereeId);
            Assert.Equal(connectedMatchId, transfer.PreviousMatchId);
            Assert.Equal(match.MatchId, transfer.FutureMatchId);
            Assert.False(transfer.FromHome);
            Assert.True(transfer.Car);
        }

        [Fact]
        public void CheckTimeAvailabilityWithTransferOfReferee_PreMatchTransferNotPossible_ReturnsFalse()
        {
            // Arrange
            var endOfPreviousMatch = new DateTime(2025, 5, 17, 13, 45, 0);
            var connectedMatchId = "M1";
            var transferLength = 45; // 45 minutes
            var refereeId = 1;
            var hasCar = true;

            var match = new Models.Match
            {
                MatchId = "M2",
                MatchDate = new DateOnly(2025, 5, 17),
                MatchTime = new TimeOnly(14, 0), // Not enough time after previous match
                Competition = new Competition { MatchLength = 45 }
            };

            // Act
            var result = _refereeService.CheckTimeAvailabilityWithTransferOfReferee(
                endOfPreviousMatch,
                connectedMatchId,
                match,
                transferLength,
                refereeId,
                true, // is pre-match 
                hasCar
            );

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.GetDataOrThrow().Item1); // Transfer is not possible
            Assert.True(result.GetDataOrThrow().Item2 > 0);
        }

        // Constants from the method for verification
        private const int STARTING_POINTS = 100;
        private const int VETO_PENALTY = -40;
        private const int TEAM_PENALTY_FIRST_TWO_AT_HOME = -3;
        private const int TEAM_PENALTY_THIRD_AT_HOME = -5;
        private const int TEAM_PENALTY_ADDITIONAL_AT_HOME = -10;
        private const int TEAM_PENALTY_FIRST_TWO_AWAY = -2;
        private const int TEAM_PENALTY_THIRD_AWAY = -3;
        private const int TEAM_PENALTY_ADDITIONAL_AWAY = -5;
        private const int DISTANCE_PENALTY_PER_5KM = -3;
        private const int WEEKEND_PENALTY_PER_MATCH_AR = -2;
        private const int WEEKEND_PENALTY_PER_MATCH_R = -3;
        [Fact]
        public void CalculatePointsForReferees_WithBasicScenario_ReturnsCorrectCalculation()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
            {
                new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 1, AwayCount = 1 }
            };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
            {
                new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 1, AwayCount = 1 }
            };
            var totalMatches = new List<RefereesMatchesResponseDto>
            {
                new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 1, asAssistantReferee = 1 }
            };
            var distanceDictionary = new Dictionary<int, int> { { 1, 10 } }; // 10km = 2*5km blocks

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.GetDataOrThrow());

            // Expected calculation:
            // Starting: 100
            // Home team penalties: 1*(-3) + 1*(-2) = -5
            // Away team penalties: 1*(-3) + 1*(-2) = -5
            // Distance penalty: (10/5) * (-3) = -6
            // Match penalties: 1*(-3) + 1*(-2) = -5
            // Total: 100 - 5 - 5 - 6 - 5 = 79
            int expectedPoints = 79;

            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
            Assert.Contains("Konečné skóre: 79", result.GetDataOrThrow()[1].Item2);
        }

        [Fact]
        public void CalculatePointsForReferees_WithNoMatches_ReturnsStartingPointsMinusDistance()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>();
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>();
            var totalMatches = new List<RefereesMatchesResponseDto>();
            var distanceDictionary = new Dictionary<int, int> { { 1, 5 } }; // 5km = 1*5km block

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);

            // Expected: 100 (starting) - 3 (distance) = 97
            int expectedPoints = 97;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
        }

        [Fact]
        public void CalculatePointsForReferees_WithThreeHomeMatches_AppliesCorrectPenalty()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 3, AwayCount = 0 }
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 0, AwayCount = 0 }
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 0, asAssistantReferee = 0 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 0 } };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);

            // Expected calculation for home team:
            // First two matches: 2 * (-3) = -6
            // Third match: -5
            // Total home penalty: -11
            // Total: 100 - 11 = 89
            int expectedPoints = 89;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
        }

        [Fact]
        public void CalculatePointsForReferees_WithFiveHomeMatches_AppliesAdditionalPenalty()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 5, AwayCount = 0 }
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 0, AwayCount = 0 }
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 0, asAssistantReferee = 0 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 0 } };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);

            // Expected calculation for home team:
            // First two matches: 2 * (-3) = -6
            // Third match: -5
            // Additional matches (4th and 5th): 2 * (-10) = -20
            // Total home penalty: -31
            // Total: 100 - 31 = 69
            int expectedPoints = 69;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
        }

        [Fact]
        public void CalculatePointsForReferees_WithSixOrMoreHomeMatches_AppliesVetoPenalty()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 6, AwayCount = 0 }
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 0, AwayCount = 0 }
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 0, asAssistantReferee = 0 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 0 } };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);

            // Expected: 100 - 40 (veto penalty) = 60
            int expectedPoints = 60;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
        }

        [Fact]
        public void CalculatePointsForReferees_WithAwayMatchPenalties_CalculatesCorrectly()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 0, AwayCount = 3 }
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 0, AwayCount = 4 }
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 0, asAssistantReferee = 0 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 0 } };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);

            // Expected calculation:
            // Home team away matches: 2*(-2) + (-3) = -7 (first two + third)
            // Away team away matches: 2*(-2) + (-3) + (-5) = -12 (first two + third + fourth)
            // Total: 100 - 7 - 10 = 81
            int expectedPoints = 81;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
        }

        [Fact]
        public void CalculatePointsForReferees_WithLargeDistance_AppliesCorrectDistancePenalty()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>();
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>();
            var totalMatches = new List<RefereesMatchesResponseDto>();
            var distanceDictionary = new Dictionary<int, int> { { 1, 47 } }; // 47km = 9*5km blocks (47/5 = 9 integer division)

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);

            // Expected: 100 - (9 * 3) = 73
            int expectedPoints = 73;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
            Assert.Contains("Body za lokaci :47 km", result.GetDataOrThrow()[1].Item2);
        }

        [Fact]
        public void CalculatePointsForReferees_WithWeekendMatches_AppliesCorrectPenalties()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>();
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>();
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 3, asAssistantReferee = 2 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 0 } };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);

            // Expected: 100 - (3*3 + 2*2) = 100 - 13 = 87
            int expectedPoints = 87;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
            Assert.Contains("Jako rozhodčí :3 zápasů", result.GetDataOrThrow()[1].Item2);
            Assert.Contains("Jako asistent :2 zápasů", result.GetDataOrThrow()[1].Item2);
        }

        [Fact]
        public void CalculatePointsForReferees_WithNegativeResult_ReturnsZero()
        {
            // Arrange - create scenario that results in negative points
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 6, AwayCount = 6 } // Veto penalties
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 6, AwayCount = 6 } // Veto penalties
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 10, asAssistantReferee = 10 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 100 } }; // Large distance

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.GetDataOrThrow()[1].Item1); // Should be clamped to 0
            Assert.Contains("Konečné skóre: 0", result.GetDataOrThrow()[1].Item2);
        }

        [Fact]
        public void CalculatePointsForReferees_WithMultipleReferees_CalculatesAllCorrectly()
        {
            // Arrange
            var refereeIds = new List<int> { 1, 2, 3 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 1, AwayCount = 0 },
            new RefereesTeamsMatchesResponseDto { RefereeId = 2, HomeCount = 2, AwayCount = 1 }
            // Referee 3 has no home matches
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 0, AwayCount = 1 },
            new RefereesTeamsMatchesResponseDto { RefereeId = 3, HomeCount = 1, AwayCount = 0 }
            // Referee 2 has no away matches in this list
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 1, asAssistantReferee = 0 },
            new RefereesMatchesResponseDto { RefereeId = 2, asReferee = 0, asAssistantReferee = 1 },
            new RefereesMatchesResponseDto { RefereeId = 3, asReferee = 2, asAssistantReferee = 2 }
        };
            var distanceDictionary = new Dictionary<int, int>
        {
            { 1, 5 },
            { 2, 10 },
            { 3, 15 }
        };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.GetDataOrThrow().Count);

            // All referees should be present
            Assert.True(result.GetDataOrThrow().ContainsKey(1));
            Assert.True(result.GetDataOrThrow().ContainsKey(2));
            Assert.True(result.GetDataOrThrow().ContainsKey(3));

            // All should have positive scores in this scenario
            Assert.True(result.GetDataOrThrow()[1].Item1 > 0);
            Assert.True(result.GetDataOrThrow()[2].Item1 > 0);
            Assert.True(result.GetDataOrThrow()[3].Item1 > 0);
        }

        [Fact]
        public void CalculatePointsForReferees_WithEmptyLists_ReturnsStartingPoints()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>();
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>();
            var totalMatches = new List<RefereesMatchesResponseDto>();
            var distanceDictionary = new Dictionary<int, int>();

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(STARTING_POINTS, result.GetDataOrThrow()[1].Item1);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, -3)]
        [InlineData(2, -6)]
        [InlineData(3, -11)] // 2*(-3) + (-5)
        [InlineData(4, -21)] // 2*(-3) + (-5) + 1*(-10)
        [InlineData(5, -31)] // 2*(-3) + (-5) + 2*(-10)
        [InlineData(6, -40)] // Veto penalty
        [InlineData(10, -40)] // Still veto penalty
        public void CalculatePointsForReferees_WithVariousHomeMatchCounts_AppliesCorrectPenalties(int homeCount, int expectedPenalty)
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = homeCount, AwayCount = 0 }
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 0, AwayCount = 0 }
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 0, asAssistantReferee = 0 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 0 } };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);
            int expectedPoints = STARTING_POINTS + expectedPenalty;
            Assert.Equal(expectedPoints, result.GetDataOrThrow()[1].Item1);
        }

        [Fact]
        public void CalculatePointsForReferees_ReasonContainsAllCalculationDetails()
        {
            // Arrange
            var refereeIds = new List<int> { 1 };
            var homeMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 2, AwayCount = 1 }
        };
            var awayMatches = new List<RefereesTeamsMatchesResponseDto>
        {
            new RefereesTeamsMatchesResponseDto { RefereeId = 1, HomeCount = 1, AwayCount = 3 }
        };
            var totalMatches = new List<RefereesMatchesResponseDto>
        {
            new RefereesMatchesResponseDto { RefereeId = 1, asReferee = 2, asAssistantReferee = 1 }
        };
            var distanceDictionary = new Dictionary<int, int> { { 1, 25 } };

            // Act
            var result = _refereeService.CalculatePointsForReferees(
                refereeIds, homeMatches, awayMatches, totalMatches, distanceDictionary);

            // Assert
            Assert.True(result.IsSuccess);
            var reason = result.GetDataOrThrow()[1].Item2;

            // Check that reason contains expected Czech text and calculations
            Assert.Contains("Domácí tým", reason);
            Assert.Contains("Hostující tým", reason);
            Assert.Contains("Body za lokaci :25 km", reason);
            Assert.Contains("Jako rozhodčí :2 zápasů", reason);
            Assert.Contains("Jako asistent :1 zápasů", reason);
            Assert.Contains("Konečné skóre:", reason);
            Assert.Contains("zápasy doma:", reason);
            Assert.Contains("vonku:", reason);
        }
    }
    public class TimeRange : RefereeWithTimeOptions.TimeRange
    {
        public TimeRange(DateTime start, DateTime end, string rangeType, string matchId = null, int? excuseId = null, int? slotId = null, int? transferId = null)
            : base(start, end, rangeType, matchId, excuseId, slotId, transferId)
        {
        }
    }
}