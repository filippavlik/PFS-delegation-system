using AdminPart.Common;
using AdminPart.Data;
using AdminPart.DTOs;
using AdminPart.Models;
using AdminPart.Services.RefereeServices;
using AdminPart.Views.ViewModels;
using Aspose.Cells.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.FileSystemGlobbing;
using Nest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;
using static AdminPart.Views.ViewModels.RefereeWithTimeOptions;

namespace AdminPart.Services.AdminServices
{
    public class AdminService : IAdminService
    {
        private readonly ILogger<AdminService> _logger;
        private readonly Data.IAdminRepo _adminRepo;
        Dictionary<string, Func<IEnumerable<MatchViewModel>, List<MatchViewModel>>> _sortStrategies;

        //these are adjustable fields
        private readonly int reserveBetweenMatches = 30;
        private readonly int reserveToSecondMatch = 75;




        public AdminService(Data.IAdminRepo adminRepo, ILogger<AdminService> logger)
        {
            _logger = logger;
            _adminRepo = adminRepo;

            _sortStrategies = new Dictionary<string, Func<IEnumerable<MatchViewModel>, List<MatchViewModel>>>
            {
                { "sortByFieldAsc", matches => SortByField(true, matches) },
                { "sortByFieldDesc", matches => SortByField(false, matches) },
                { "sortByGameTimeAsc", matches => SortByGameTime(true, matches) },
                { "sortByGameTimeDesc", matches => SortByGameTime(false, matches) },
                { "sortByNameCategoryAsc", matches => SortByCategory(true, matches) },
                { "sortByNameCategoryDesc", matches => SortByCategory(false, matches) },
                { "sortByNameHomeTeamAsc", matches => SortByHomeTeam(true, matches) },
                { "sortByNameHomeTeamDesc", matches => SortByHomeTeam(false, matches) },
                { "sortByNameAwayTeamAsc", matches => SortByAwayTeam(true, matches) },
                { "sortByNameAwayTeamDesc",matches => SortByAwayTeam(false, matches)  },
                { "sortByUndelegatedMatches",matches => SortByUndelegatedMatches(matches)  }              
            };
        }


        public ServiceResult<List<Models.Match>> ProccessDtosToMatches(List<UnfilledMatchDto> listOfMatches,string user)
        {
            try
            {
                List<Models.Match> resultList = new List<Models.Match>();
                foreach (var matchDto in listOfMatches)
                {
                    // The competition code is extracted from the first 10 characters of NumberMatch
                    string competitionCode = !string.IsNullOrEmpty(matchDto.NumberMatch) && matchDto.NumberMatch.Length >= 10
                                ? matchDto.NumberMatch.Substring(0, 10)
                                : "";
                    var competition = _adminRepo.DoesCompetitionExist(competitionCode).GetDataOrThrow(); //it will return competition or default competition 1
                    var resultWithField = _adminRepo.GetOrSaveTheField(matchDto.GameField).GetDataOrThrow();

                    var doesHomeTeamExists = _adminRepo.GetOrSaveTheTeam(matchDto.IdHomeRaw, matchDto.NameHome).GetDataOrThrow;
                    var doesAwayTeamExists = _adminRepo.GetOrSaveTheTeam(matchDto.IdAwayRaw, matchDto.NameAway).GetDataOrThrow;

                    var matchDate = DateOnly.FromDateTime(matchDto.DateOfGame);
                    var matchTime = TimeOnly.FromDateTime(matchDto.DateOfGame);

                    var timestampAdded = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    //we want to have timestamp for Prague time

                    var match = new Models.Match
                    {
                        MatchId = matchDto.NumberMatch,
                        CompetitionId = competition.CompetitionId,
                        HomeTeamId = matchDto.IdHomeRaw,
                        AwayTeamId = matchDto.IdAwayRaw,
                        FieldId = resultWithField.FieldId,
                        MatchDate = matchDate,
                        MatchTime = matchTime,
                        AlreadyPlayed = false,
                        Locked = false,
                        LastChangedBy = user, 
                        LastChanged = timestampAdded,
                        Competition = competition
                    };

                    resultList.Add(match);
                }
                return ServiceResult<List<Models.Match>>.Success(resultList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProccessDtosToMatches] Error transfering dtos to object match");
                return ServiceResult<List<Models.Match>>.Failure("Nepodařilo se spracovat zápasy!");
            }
        }
	    public ServiceResult<int> CalculateAverageDistance(Tuple<float,float> ? locationBefore, Tuple<float, float>? locationAfter,Models.Match match) {
            try
            {
                int kmCalculationBefore = 0;
                int kmCalculationAfter = 0;
                int kmTogether = 0;
                int count = 0;
                const float epsilon = 1e-6f;
                if (locationBefore != null && Math.Abs(locationBefore.Item1) > epsilon &&
                    Math.Abs(locationBefore.Item2) > epsilon)
                {
                    count++;
                    var sCoord = new System.Device.Location.GeoCoordinate(locationBefore.Item1, locationBefore.Item2);
                    var eCoord = new System.Device.Location.GeoCoordinate(match.Field.Latitude, match.Field.Longitude);

                    kmCalculationBefore = (int)(sCoord.GetDistanceTo(eCoord) / 1000);
                    kmTogether += kmCalculationBefore;
                }

                if (locationAfter != null && Math.Abs(locationAfter.Item1) > epsilon &&
                    Math.Abs(locationAfter.Item2) > epsilon)
                {
                    count++;
                    var sCoord = new System.Device.Location.GeoCoordinate(locationAfter.Item1, locationAfter.Item2);
                    var eCoord = new System.Device.Location.GeoCoordinate(match.Field.Latitude, match.Field.Longitude);

                    kmCalculationAfter = (int)(sCoord.GetDistanceTo(eCoord) / 1000);
                    kmTogether += kmCalculationAfter;

                }

                if (count == 0)
                {
                    return ServiceResult<int>.Success(0);
                }
                else
                {
                    return ServiceResult<int>.Success(kmTogether / count); //average
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[CalculateAverageDistance] Error in calculating aproximated distance");
                return ServiceResult<int>.Failure("Chyba při získávání přibližné vzdálenosti u rozhodčích");
            }
        }

        public async Task<ServiceResult<List<RefereesTeamsMatchesResponseDto>>> GetRefereeMatchStatsAsync(RefereesTeamsMatchesRequestDto request)
        {
            try
            {
                var queryResult = (await _adminRepo.GetTeamsPureMatchesAsync(request.TeamId, request.CompetitionId))
                    .GetDataOrThrow();

                // 1. When IsReferee is true: Process main referee assignments
                // 2. When IsReferee is false: Process assistant referee assignments
                var matchRefereeStats = request.IsReferee
                    ? queryResult
                        .Where(m => request.RefereeIds.Contains(m.RefereeId ?? -1))
                        .GroupBy(m => m.RefereeId ?? -1)
                        .ToDictionary(
                            g => g.Key,
                            g => new
                            {
                                HomeCount = g.Count(m => m.HomeTeamId == request.TeamId),
                                AwayCount = g.Count(m => m.AwayTeamId == request.TeamId)
                            })
                    : queryResult
                        .Where(m =>
                            (m.Ar1Id.HasValue && request.RefereeIds.Contains(m.Ar1Id.Value)) ||
                            (m.Ar2Id.HasValue && request.RefereeIds.Contains(m.Ar2Id.Value)))
                        .SelectMany(m => new[]
                        {
                        new { RefereeId = m.Ar1Id, IsHome = m.HomeTeamId == request.TeamId, IsAway = m.AwayTeamId == request.TeamId },
                        new { RefereeId = m.Ar2Id, IsHome = m.HomeTeamId == request.TeamId, IsAway = m.AwayTeamId == request.TeamId }
                        })
                        .Where(x => x.RefereeId.HasValue && request.RefereeIds.Contains(x.RefereeId.Value))
                        .GroupBy(x => x.RefereeId.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => new
                            {
                                HomeCount = g.Count(x => x.IsHome),
                                AwayCount = g.Count(x => x.IsAway)
                            });

                var result = new List<RefereesTeamsMatchesResponseDto>();

                foreach (var refId in request.RefereeIds)
                {
                    // Important domain logic: If a team has vetoed a referee, special count values (6)
                    // are returned regardless of actual match history. This appears to be a business rule.
                    bool vetoExists = _adminRepo.DoesVetoExistForTeam(request.TeamId, request.CompetitionId, refId)
                        .GetDataOrThrow();

                    if (vetoExists)
                    {
                        result.Add(new RefereesTeamsMatchesResponseDto
                        {
                            RefereeId = refId,
                            HomeCount = 6, // Special value for vetoed referees
                            AwayCount = 6 // Special value for vetoed referees
                        });
                    }
                    else
                    {
                        matchRefereeStats.TryGetValue(refId, out var stats);
                        result.Add(new RefereesTeamsMatchesResponseDto
                        {
                            RefereeId = refId,
                            HomeCount = stats?.HomeCount ?? 0,
                            AwayCount = stats?.AwayCount ?? 0
                        });
                    }
                }

                return ServiceResult<List<RefereesTeamsMatchesResponseDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RefereeStatsService] Error in GetRefereeMatchStatsAsync");
                return ServiceResult<List<RefereesTeamsMatchesResponseDto>>.Failure("Nepodařilo se získat data rozhodčích o delegovaných zápasů týmů!");
            }


        }
        public ServiceResult<List<MatchViewModel>> MakeConnectionsOfMatches(List<MatchViewModel> matches)
        {
            try
            {
                var sortedMatchesByField = SortByField(true, matches);

                for (int i = 0; i < sortedMatchesByField.Count - 1; i++)
                {
                    Models.Match currentMatch = sortedMatchesByField[i].Match;
                    Models.Match nextMatch = sortedMatchesByField[i + 1].Match;

                    // Calculates the end time of a match based on competition rules:
                    // - MatchLength is the length of each half
                    // - Multiplied by 2 for both halves
                    // - Plus 15 minutes for halftime break
                    var lengthOfGame = currentMatch.Competition.MatchLength * 2 + 15;
                    DateTime endTimeOfGame = currentMatch.MatchDate.ToDateTime(currentMatch.MatchTime).AddMinutes(lengthOfGame);

                    // Define acceptable time window for the next match to start:
                    // - Lower bound: Minimum time needed between matches (reserveBetweenMatches)
                    // - Upper bound: Maximum allowed time gap (reserveToSecondMatch)
                    DateTime toleratedStartOfTheNextLowerBound = endTimeOfGame.AddMinutes(reserveBetweenMatches);
                    DateTime toleratedStartOfTheNextUpperBound = endTimeOfGame.AddMinutes(reserveToSecondMatch);


                    // Two conditions must be met to connect matches:
                    // 1. The next match must start within the defined time window after the current match ends
                    // 2. Both matches must be played on the same field
                    bool startsAfterCurrentEnds = nextMatch.MatchDate.ToDateTime(nextMatch.MatchTime) > toleratedStartOfTheNextLowerBound
                                                  && nextMatch.MatchDate.ToDateTime(nextMatch.MatchTime) < toleratedStartOfTheNextUpperBound;

                    bool sameField = nextMatch.FieldId.Equals(currentMatch.FieldId);

                    // If conditions are met, create bidirectional links between the matches
                    if (startsAfterCurrentEnds && sameField)
                    {
                        currentMatch.PostMatch = nextMatch.MatchId;
                        nextMatch.PreMatch = currentMatch.MatchId;
                    }
                }

                return ServiceResult<List<MatchViewModel>>.Success(sortedMatchesByField);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MakeConnectionsOfMatches] Error in proccess of connecting matches");
                return ServiceResult<List<MatchViewModel>>.Failure("Chyba při spojování zápasů");
            }
        }
        public int GetPercentageOfDelegatedMatches(List<MatchViewModel> matches)
        {
            int totalMatches = matches.Count;

            // A match is considered "delegated" if it has any referee assigned
            // (main referee or either of the assistant referees)
            int delegatedMatches = matches.Count(match =>
                match.Match.RefereeId.HasValue ||
                match.Match.Ar1Id.HasValue ||
                match.Match.Ar2Id.HasValue
            );

            if (totalMatches == 0) return 0; 

            int percentage = (int)((delegatedMatches / (double)totalMatches) * 100);
            return percentage;
        }

        public ServiceResult<List<MatchViewModel>> SortMatches(string sortKey, IEnumerable<MatchViewModel> matches)
        {
            try
            {
                // This method uses a strategy pattern for sorting, where different sort strategies
                // are stored in a dictionary (_sortStrategies) with string keys
                // This allows for flexible sorting options without complex if/else logic
                if (_sortStrategies.TryGetValue(sortKey, out var sortFunc))
                {
                    return ServiceResult<List<MatchViewModel>>.Success(sortFunc(matches));
                }

                return ServiceResult<List<MatchViewModel>>.Success(SortByGameTime(true, matches));

            } catch(Exception ex)
            {
                _logger.LogError(ex, "[SortMatches] Error in proccess of sorting matches");
                return ServiceResult<List<MatchViewModel>>.Failure("Chyba při třídění zápasů");
            }
        }
        public List<MatchViewModel> SortBy<TKey>(IEnumerable<MatchViewModel> matches, Func<MatchViewModel, TKey> selector, bool asc)
        {
            return asc
            ? matches.OrderBy(selector).ToList()
            : matches.OrderByDescending(selector).ToList();
        }

        public List<MatchViewModel> SortByField(bool asc, IEnumerable<MatchViewModel> matches)
        {
            var sorted = asc
               ? matches.OrderBy(m => m.FieldName).ThenBy(m => m.Match.MatchDate).ThenBy(m => m.Match.MatchTime)
               : matches.OrderByDescending(m => m.FieldName).ThenBy(m => m.Match.MatchTime).ThenBy(m => m.Match.MatchTime);
            return sorted.ToList();
        }

        public List<MatchViewModel> SortByGameTime(bool asc, IEnumerable<MatchViewModel> matches)
        {
            return SortBy(matches,
                m => (m.Match.MatchDate, m.Match.MatchTime),
                asc);
        }
        
        public List<MatchViewModel> SortByHomeTeam(bool asc, IEnumerable<MatchViewModel> matches)
        {
            return SortBy(matches, m => (m.HomeTeamName,m.Match.MatchDate, m.Match.MatchTime), asc);
        }

        public List<MatchViewModel> SortByAwayTeam(bool asc, IEnumerable<MatchViewModel> matches)
        {
            return SortBy(matches, m => (m.AwayTeamName, m.Match.MatchDate, m.Match.MatchTime), asc);
        }

        public List<MatchViewModel> SortByCategory(bool asc, IEnumerable<MatchViewModel> matches)
        {
            var sorted = asc
                   ? matches.OrderBy(m => ((m.CompetitionName.IndexOf("&") > 0 ? m.CompetitionName.Substring(m.CompetitionName.IndexOf("&") + 1) : m.CompetitionName),
                                            m.Match.MatchDate,m.Match.MatchTime))
                                                    .ThenBy(m => m.Match.MatchDate)
                                                    .ThenBy(m => m.Match.MatchTime)
                   : matches.OrderByDescending(m => (m.CompetitionName.IndexOf("&") > 0 ? m.CompetitionName.Substring(m.CompetitionName.IndexOf("&") + 1) : m.CompetitionName))
                                                    .ThenBy(m => m.Match.MatchDate)
                                                    .ThenBy(m => m.Match.MatchTime);


            return sorted.ToList();
        }
        public List<MatchViewModel> SortByUndelegatedMatches(IEnumerable<MatchViewModel> matches)
        {
            return matches
                .OrderBy(m => new[] { m.Match.RefereeId, m.Match.Ar1Id, m.Match.Ar2Id }
                    .Count(id => id != null))
                .ThenBy(m => m.Match.MatchDate)
                .ThenBy(m => m.Match.MatchTime)
                .ToList();
        }



    }
}
