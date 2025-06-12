using AdminPart.Common;
using AdminPart.DTOs;
using AdminPart.Models;
using AdminPart.Views.ViewModels;
using Aspose.Cells;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing;
using SkiaSharp;
using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using static AdminPart.Services.RefereeServices.RefereeService;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AdminPart.Data
{
    public class AdminRepo : IAdminRepo
    {

        private readonly AdminDbContext _context;
        private readonly ILogger<AdminRepo> _logger;


        public AdminRepo(ILogger<AdminRepo> logger, AdminDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        public RepositoryResult<Field> GetOrSaveTheField(string fieldName)
        {
            try
            {
                var existingRelation = _context.Fields
                                        .Where(s => s.FieldName == fieldName)
                                        .FirstOrDefault();
                if (existingRelation != null)
                {
                    return RepositoryResult<Field>.Success(existingRelation);
                }
                else
                {
                    Field newFieldToAdd = new Field(fieldName);
                    _context.Fields.Add(newFieldToAdd);
                    _context.SaveChanges();

                    return RepositoryResult<Field>.Success(newFieldToAdd);
                }

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[GetOrSaveTheFieldAsync] Error saving field");
                return RepositoryResult<Field>.Failure("Nepodařilo se uložiť hřište do databázy");

            }
        }
        public RepositoryResult<Team> GetOrSaveTheTeam(string id, string name)
        {
            try
            {
                var existingRelation = _context.Teams
                                        .Where(s => s.TeamId == id)
                                        .FirstOrDefault();
                if (existingRelation != null)
                {
                    return RepositoryResult<Team>.Success(existingRelation);
                }
                else
                {
                    Team newTeamToAdd = new Team
                    {
                        TeamId = id,
                        Name = name,
                    };
                    _context.Teams.Add(newTeamToAdd);
                    _context.SaveChanges();

                    return RepositoryResult<Team>.Success(newTeamToAdd);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetOrSaveTheTeam] Error saving team");
                return RepositoryResult<Team>.Failure("Nepodařilo se uložiť tím"+ name +"do databázy");

            }
        }
        public RepositoryResult<Competition> DoesCompetitionExist(string idOfCompetition)
        {
            try
            {
                var existingRelation = _context.Competitions
                                        .Where(s => s.CompetitionId == idOfCompetition)
                                        .FirstOrDefault();
                if (existingRelation != null)
                {
                    return RepositoryResult<Competition>.Success(existingRelation);
                }
                else
                {
                    var defaultCompetition = _context.Competitions
                                        .Where(s => s.CompetitionId == "1")
                                        .FirstOrDefault();
                    return RepositoryResult<Competition>.Success(defaultCompetition);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DoesCompetitionExist] Error checking competition");
                return RepositoryResult<Competition>.Failure("Nepodařilo se zjistiť či existuje soutež s id:"+idOfCompetition);

            }
        }
        public async Task<RepositoryResult<bool>> DoesVetoExist(string matchId, int refereeId)
        {
            try
            {
                var match = (await GetMatchByIdAsync(matchId)).GetDataOrThrow();
                var competitionId = match.CompetitionId;
                var homeTeam = match.HomeTeamId;
                var awayTeam = match.AwayTeamId;

                bool vetoExistsForAllCategories = await _context.Vetoes.AnyAsync(v =>
                   v.CompetitionId == "all" &&
                   (v.TeamId == match.HomeTeamId || v.TeamId == match.AwayTeamId) &&
                   v.RefereeId == refereeId);

                if (vetoExistsForAllCategories)
                {
                    return RepositoryResult<bool>.Success(true);
                }


                bool vetoExists = await _context.Vetoes.AnyAsync(v =>
                    v.CompetitionId == match.CompetitionId &&
                    (v.TeamId == match.HomeTeamId || v.TeamId == match.AwayTeamId) &&
                    v.RefereeId == refereeId);

                return RepositoryResult<bool>.Success(vetoExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DoesVetoExist] Error finding out if the veto for referee exists");
                return RepositoryResult<bool>.Failure("Nepodařilo se získat zda rozhodčí má nahráne veto pro tyto tímy!");
            }
        }
        public RepositoryResponse DoesMatchExists(string matchId)
        {
            try
            {
                var existingRelation = _context.Matches
                                        .Where(m => m.MatchId == matchId)
                                        .FirstOrDefault();
                if (existingRelation != null)
                {
                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Zapas existuje!"
                    };
                }
                else
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Zapas neexistuje!"
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DoesMatchExists] Error checking match");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Nepodařilo se získat zda zápas existuje! !"
                };

            }
        }

        public RepositoryResult<bool> DoesVetoExistForTeam(string teamId,string competitionId, int refereeId)
        {
            try
            {
                bool vetoExists = _context.Vetoes.Any(v =>
                   (v.CompetitionId == "all" || v.CompetitionId == competitionId) &&
                   (v.TeamId == teamId) &&
                   v.RefereeId == refereeId);

                return RepositoryResult<bool>.Success(vetoExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DoesVetoExistForTeam] Error finding out if the veto for referee exists");
                return RepositoryResult<bool>.Failure("Nepodařilo se získat zda rozhodčí má nahráne veto pro tento tímy!");
            }
        }

        public RepositoryResponse UploadStartGameDate(DateOnly date)
        {
            try
            {
                var existing = _context.StartingGameDates.Find(1);


                if (existing == null)
                {
                    var gameDate = new StartingGameDate { GameDateId = 1, GameDate = date };
                    _context.StartingGameDates.Add(gameDate);
                }
                else
                {
                    existing.GameDate = date;
                    _context.StartingGameDates.Update(existing);
                }

                _context.SaveChanges();

                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Datum nahraný úspěšně!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadStartGameDate] Error uploading date.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Nastala chyba pri přidávaní datumu!"
                };
            }
        }
        public RepositoryResult<DateOnly> GetStartGameDate()
        {
            try
            {
                var existingRelation = _context.StartingGameDates
                                        .Where(s => s.GameDateId == 1)
                                        .FirstOrDefault();
                if (existingRelation != null)
                {
                    /*if (existingRelation.GameDate.CompareTo(DateOnly.FromDateTime(DateTime.Now)) < 0) do production
                    {
                        return RepositoryResult<DateOnly>.Failure("Datum herního víkendu již prebehl , správce zapomněl nastavit nové datum!");
                    }
                    else
                    {*/
                        return RepositoryResult<DateOnly>.Success(existingRelation.GameDate);
                   // }
                }
                else
                {
                    return RepositoryResult<DateOnly>.Failure("Nebylo nalezeno žádné datum");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetStartGameDate] Error getting game time");
                return RepositoryResult<DateOnly>.Failure("Nepodařilo se získat datum , neznáma chyba");

            }
        }

        public async Task<RepositoryResult<Models.Match>> GetMatchByIdAsync(string id)
        {
            try
            {
                var match = await _context.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.Field)
                    .FirstOrDefaultAsync(m => m.MatchId == id);

                if (match == null)
                {
                    return RepositoryResult<Models.Match>.Failure("Zápas s id " + id + " nebyl najden!");
                }

                return RepositoryResult<Models.Match>.Success(match);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetMatchByIdAsync] Error getting match");
                return RepositoryResult<Models.Match>.Failure("Nepodařilo se získat zápas z databáze");
            }
        }

        //this returns all future matches, sometimes we need only weekend
        public async Task<RepositoryResult<List<Models.Match>>> GetPureMatchesAsync()
        {
            try
            {
		DateOnly startDateOfGamePeriod  = GetStartGameDate().GetDataOrThrow();
                var matches = await _context.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.Field)
		    .Where(m => m.MatchDate >= startDateOfGamePeriod.AddDays(-1) && m.MatchDate <= startDateOfGamePeriod.AddDays(3))
                //production .Where(m => m.StartDate.ToDateTime(m.StartTime) > DateTime.Now())
                .ToListAsync();

                return RepositoryResult<List<Models.Match>>.Success(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetPureMatchesAsync] Error downloading the pure matches.");
                return RepositoryResult<List<Models.Match>>.Failure("Nepodařilo se získat čisté zápasy z serveru.");
            }
        }
        public async Task<RepositoryResult<List<Models.Match>>> GetPureNotPlayedMatchesAsync()
        {
            try
            {
                var matches = await _context.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.Field)
                    .Where(m => !m.AlreadyPlayed)
                .ToListAsync();

                return RepositoryResult<List<Models.Match>>.Success(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetPureNotPlayedMatchesAsync] Error downloading the pure matches.");
                return RepositoryResult<List<Models.Match>>.Failure("Nepodařilo se získat čisté zápasy z serveru.");
            }
        }
        public async Task<RepositoryResult<List<Models.Match>>> GetTeamsPureMatchesAsync(string teamId,string competitionId)
        {
            try
            {
                var matches = await _context.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.Field)
                    .Where(m => m.AlreadyPlayed &&
                            (m.HomeTeamId == teamId || m.AwayTeamId == teamId) &&
                            m.CompetitionId == competitionId
                            )
                    .ToListAsync();

                return RepositoryResult<List<Models.Match>>.Success(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetTeamsPureMatchesAsync] Error downloading the pure teams matches.");
                return RepositoryResult<List<Models.Match>>.Failure("Nepodařilo se získat čisté zápasy z serveru.");
            }

        }
        public async Task<RepositoryResult<List<Models.Team>>> GetTeamsByInput(string input)
        {
            try
            {
                var teams = await _context.Teams
                    .Where(t => EF.Functions.Like(t.Name.ToLower(), $"%{input.ToLower()}%"))
                    .ToListAsync();

                return RepositoryResult<List<Models.Team>>.Success(teams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetTeamsByInput] Error fetching teams by input.");
                return RepositoryResult<List<Models.Team>>.Failure("Nepodařilo se získat tímy podle zadaného vstupu.");
            }
        }

        public async Task<RepositoryResult<List<Models.Veto>>> GetRefereesVetoesAsync(int refereeId)
        {
            try
            {
                var vetoes = await _context.Vetoes
                    .Include(v => v.Competition)
                    .Include(v => v.Team)
                    .Where(v =>
                            (v.RefereeId == refereeId)
                          )
                    .ToListAsync();

                return RepositoryResult<List<Models.Veto>>.Success(vetoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereesVetoesAsync] Error downloading the referees vetoes.");
                return RepositoryResult<List<Models.Veto>>.Failure("Nepodařilo se získat veta rozhodčího z serveru.");
            }

        }
        public async Task<RepositoryResult<List<Models.Transfer>>> GetRefereesTransfersAsync(int refereeId)
        {
            try
            {
                var transfers = await _context.Transfers
                    .Where(v =>
                            (v.RefereeId == refereeId)
                          )
                    .ToListAsync();

                return RepositoryResult<List<Models.Transfer>>.Success(transfers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereesTrasnfersAsync] Error downloading the referees transfers.");
                return RepositoryResult<List<Models.Transfer>>.Failure("Nepodařilo se získat transfery rozhodčího z serveru.");
            }
        }
        public async Task<RepositoryResult<List<Models.Transfer>>> GetTransfersWithinGameWeekend(DateTime startDayOfWeekend)

        {
            try
            {
                var transfers = await _context.Transfers
                    .Where(t => t.ExpectedDeparture >= startDayOfWeekend &&
                        t.ExpectedArrival <= startDayOfWeekend.AddDays(4))
                .ToListAsync();


                return RepositoryResult<List<Models.Transfer>>.Success(transfers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetTransfersWithinGameWeekend] Error downloading the transfers.");
                return RepositoryResult<List<Models.Transfer>>.Failure("Nepodařilo se získat transfery dle data z serveru.");
            }
        }



        public async Task<RepositoryResult<List<MatchViewModel>>> GetMatchesAsync(Dictionary<int,string> dictNameOfReferees)
        {
            try
            {
                DateOnly firstGameDay = GetStartGameDate().Data;
                var saturdayStart = firstGameDay.ToDateTime(new TimeOnly(0, 1));
                var saturdayNoon = firstGameDay.ToDateTime(new TimeOnly(12, 0));

                var sunday = firstGameDay.AddDays(1);
                var sundayStart = sunday.ToDateTime(new TimeOnly(0, 1));
                var sundayNoon = sunday.ToDateTime(new TimeOnly(12, 0));
                var sundayEnd = sunday.ToDateTime(new TimeOnly(23, 59));

                var matches = await _context.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.Field)
                    //production .Where(m => m.StartDate.ToDateTime(m.StartTime) > DateTime.Now())
                .ToListAsync();


                // Project the result into the MatchViewModel
                var matchViewModels = matches.Select(match => new MatchViewModel
                {
                    Match = match,
                    CompetitionName = match.Competition.CompetitionName,
                    FieldName = _context.Fields.FirstOrDefault(t => t.FieldId == match.FieldId)?.FieldName ?? "Neznáme hřište",
                    HomeTeamName = _context.Teams.FirstOrDefault(t => t.TeamId == match.HomeTeamId)?.Name ?? "Neznámy tím",
                    AwayTeamName = _context.Teams.FirstOrDefault(t => t.TeamId == match.AwayTeamId)?.Name ?? "Neznámy tím",
                    RefereeName = match.RefereeId.HasValue && dictNameOfReferees.TryGetValue(match.RefereeId.Value, out string refereeName)
                                        ? refereeName
                                        : null,
                    Ar1Name = match.Ar1Id.HasValue && dictNameOfReferees.TryGetValue(match.Ar1Id.Value, out string ar1Name)
                                        ? ar1Name
                                        : null,
                    Ar2Name = match.Ar2Id.HasValue && dictNameOfReferees.TryGetValue(match.Ar2Id.Value, out string ar2Name)
                                        ? ar2Name
                                        : null,
                    WeekendPartColor = GetMatchTimeColor(match.MatchDate.ToDateTime(match.MatchTime), saturdayStart, saturdayNoon, sundayStart, sundayNoon, sundayEnd)

                }).ToList();

                return RepositoryResult<List<MatchViewModel>>.Success(matchViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetMatchesAsync] Error downloading the matches.");
                return RepositoryResult<List<MatchViewModel>>.Failure("Nepodařilo se získat zápasy z serveru.");
            }
        }
        public async Task<RepositoryResult<List<MatchViewModel>>> GetMatchesByDateAsync(Dictionary<int, string> dictNameOfReferees,DateTime startDate,DateTime endDate)
        {
            try
            {
                DateOnly firstGameDay = GetStartGameDate().Data;
                var saturdayStart = firstGameDay.ToDateTime(new TimeOnly(0, 1));
                var saturdayNoon = firstGameDay.ToDateTime(new TimeOnly(12, 0));

                var sunday = firstGameDay.AddDays(1);
                var sundayStart = sunday.ToDateTime(new TimeOnly(0, 1));
                var sundayNoon = sunday.ToDateTime(new TimeOnly(12, 0));
                var sundayEnd = sunday.ToDateTime(new TimeOnly(23, 59));

                var matches = await _context.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.Field)
                    .Where(m => m.MatchDate.ToDateTime(m.MatchTime) >= startDate &&
                        m.MatchDate.ToDateTime(m.MatchTime) <= endDate)
                .ToListAsync();


                // Project the result into the MatchViewModel
                var matchViewModels = matches.Select(match => new MatchViewModel
                {
                    Match = match,
                    CompetitionName = match.Competition.CompetitionName,
                    FieldName = _context.Fields.FirstOrDefault(t => t.FieldId == match.FieldId)?.FieldName ?? "Neznáme hřište",
                    HomeTeamName = _context.Teams.FirstOrDefault(t => t.TeamId == match.HomeTeamId)?.Name ?? "Neznámy tím",
                    AwayTeamName = _context.Teams.FirstOrDefault(t => t.TeamId == match.AwayTeamId)?.Name ?? "Neznámy tím",
                    RefereeName = match.RefereeId.HasValue && dictNameOfReferees.TryGetValue(match.RefereeId.Value, out string refereeName)
                                        ? refereeName
                                        : null,
                    Ar1Name = match.Ar1Id.HasValue && dictNameOfReferees.TryGetValue(match.Ar1Id.Value, out string ar1Name)
                                        ? ar1Name
                                        : null,
                    Ar2Name = match.Ar2Id.HasValue && dictNameOfReferees.TryGetValue(match.Ar2Id.Value, out string ar2Name)
                                        ? ar2Name
                                        : null,
                    WeekendPartColor = GetMatchTimeColor(match.MatchDate.ToDateTime(match.MatchTime), saturdayStart, saturdayNoon, sundayStart, sundayNoon, sundayEnd)

                }).ToList();

                return RepositoryResult<List<MatchViewModel>>.Success(matchViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetMatchesByDateAsync] Error downloading the matches.");
                return RepositoryResult<List<MatchViewModel>>.Failure("Nepodařilo se získat zápasy dle data z serveru.");
            }
        }
        public async Task<RepositoryResponse> AddMatchesAsync(List<Models.Match> listOfMatches)
        {
            try
            {
                var newMatchIds = listOfMatches.Select(m => m.MatchId).ToList();

                // Check if any of these IDs already exist in the database
                var existingIds = await _context.Matches
                    .Where(m => newMatchIds.Contains(m.MatchId))
                    .Select(m => m.MatchId)
                    .ToListAsync();

                if (existingIds.Any())
                {
                    //remove the duplicities
                    listOfMatches = listOfMatches.Where(m => !existingIds.Contains(m.MatchId)).ToList();
                }

                await _context.Matches.AddRangeAsync(listOfMatches);
                await _context.SaveChangesAsync();

                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Zápasy nahrané úspěšně!"
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddMatchesAsync] Error uploading matches. Count: {Count}", listOfMatches?.Count ?? 0);
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Nastala chyba pri přidávaní zápasů!"
                };
            }
        }
        public async Task<RepositoryResponse> UpdateMatchesAsync(List<Models.Match> listOfMatches)
        {
            try
            {
                foreach (var match in listOfMatches)
                {
                     _context.Matches.Update(match);
                }

                await _context.SaveChangesAsync();

                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Zápasy úspěšně pospojovány!."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateMatchesAsync] Error uploading matches. Count: {Count}", listOfMatches?.Count ?? 0);

                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Zápasy neúspěšně pospojovány (chyba s databází)!"
                };
            }

        }
        public async Task<RepositoryResult<bool>> UpdateMatchLockAsync(string id,string user)
        {
            try
            {
                var match = await _context.Matches
                   .FirstOrDefaultAsync(m => m.MatchId == id);

                if (match == null)
                {
                    return RepositoryResult<bool>.Failure("Zápas s id " + id + " nebyl najden!");
                }

                bool currentValue = match.Locked;
                match.Locked = !currentValue;
		match.LastChangedBy = user;
		DateTime timestampChange = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

		
		match.LastChanged = timestampChange;
                await _context.SaveChangesAsync();
                return RepositoryResult<bool>.Success(!currentValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateMatchLockAsync] Error uploading match!");
                return RepositoryResult<bool>.Failure("Nepodařilo se změnit stav zápasu na zamčený/odemčený!");

            }

        }
        public async Task<RepositoryResponse> UpdateMatchPlayedAsync(string id,string user)
        {
            try
            {
                var match = await _context.Matches
                   .FirstOrDefaultAsync(m => m.MatchId == id);

                if (match == null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Zápas s id " + id + " nebyl najden!"
                    };
                }

                match.AlreadyPlayed = true;
		match.LastChangedBy = user;
		DateTime timestampChange = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

		match.LastChanged = timestampChange;

                await _context.SaveChangesAsync();
                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Úspešne se podařilo změnit stav zápasu na odohratý!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateMatchPlayedAsync] Error uploading match!");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Nepodařilo se změnit stav zápasu na odohratý!"
                };
            }

        }

        public async Task<RepositoryResponse> TieAndUpdateTheMatchesAsync(List<FilledMatchDto> listOfMatches, Dictionary<string, int> refereeDict, string filePath,string user)
        {
            try
            {
                // Convert UTC time to Central European Time for consistent timestamping
                DateTime timestampChange = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

                DateOnly? earliestDate = null;
                DateOnly? latestDate = null;
                int changed = 0;

                foreach (var filledMatch in listOfMatches)
                {
                    var match = await _context.Matches.FirstOrDefaultAsync(m => m.MatchId == filledMatch.NumberMatch);

                    if (match == null)
                    {
                        continue; // Skip non-existent matches
                    }

                    // Track the range of match dates being modified
                    if (match.MatchDate != null)
                    {
                        if (earliestDate == null || match.MatchDate < earliestDate)
                        {
                            earliestDate = match.MatchDate;
                        }

                        if (latestDate == null || match.MatchDate > latestDate)
                        {
                            latestDate = match.MatchDate;
                        }
                    }

                    // Map string referee IDs to internal integer IDs
                    if (!string.IsNullOrEmpty(filledMatch.IdOfReferee))
                    {
                        if (refereeDict.TryGetValue(filledMatch.IdOfReferee, out var refereeId))
                        {
                            match.RefereeId = refereeId;
                        }
                    }else
		    {
		    	match.RefereeId = null;
		    }

                    if (!string.IsNullOrEmpty(filledMatch.IdOfAr1))
                    {
                        if (refereeDict.TryGetValue(filledMatch.IdOfAr1, out var ar1Id))
                        {
                            match.Ar1Id = ar1Id;
                        }
                    }else
                    {
                        match.Ar1Id = null;
                    }

                    if (!string.IsNullOrEmpty(filledMatch.IdOfAr2))
                    {
                        if (refereeDict.TryGetValue(filledMatch.IdOfAr2, out var ar2Id))
                        {
                            match.Ar2Id = ar2Id;
                        }
                    }else
                    {
                        match.Ar2Id = null;
                    }

                    match.AlreadyPlayed = true;
                    match.LastChanged = timestampChange;
                    changed++;

                    match.LastChangedBy = user;
                }

                if (changed != 0)
                {
                    // Log the change via some file logging or history mechanism
                    var resultOfTransaction = AddNewFilePreviousDelegation(filePath, listOfMatches.Count, earliestDate.HasValue ? earliestDate.Value : null, latestDate.HasValue ? latestDate.Value : null);

                    if (resultOfTransaction.Success)
                    {
                        await _context.SaveChangesAsync();
                        return new RepositoryResponse
                        {
                            Success = true,
                            Message = "Zápasy úspěšne uloženy!"
                        };
                    }
                }

                // No changes made
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Žádné zápasy nezměneny (chyba s databází)!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TieAndUpdateTheMatches] Error uploading played matches. Count: {Count}", listOfMatches?.Count ?? 0);

                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Odehrané zápasy neúspěšně nahrány (chyba s databází)!"
                };
            }
        }
        public async Task<RepositoryResponse> UpdateFieldsAsync(List<FilledFieldDto> listOfFields)
        {
            try
            {
                int changed = 0;
                foreach (var filledField in listOfFields)
                {
                    var field = await _context.Fields.FirstOrDefaultAsync(f => f.FieldName == filledField.FieldName);

                    if (field == null)
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(filledField.FieldAddress))
                    {
                        field.FieldAddress = filledField.FieldAddress;
                    }

                    if (filledField.FieldLatitude!= null)
                    {
                        field.Latitude = filledField.FieldLatitude.Value;
                    }

                    if (filledField.FieldLongtitude != null)
                    {
                        field.Longitude = filledField.FieldLongtitude.Value;
                    }

                    changed++;
                }
                if (changed != 0)
                {

                    await _context.SaveChangesAsync();
                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Hřište úspěšne aktualizovány! v počte " + changed
                    };
                }

                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Žádné informace o hřištech nezměneny (chyba s databází)!"
                };


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateFieldsAsync] Error uploading info about fields. Count: {Count}", listOfFields?.Count ?? 0);

                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Informace o hřištích neúspěšně nahrány (chyba s databází)!"
                };

            }
        }
        public RepositoryResponse UpdateExistingField(FieldToUpdateDto field)
        {
            try
            {
                var existing = _context.Fields.FirstOrDefault(f => f.FieldId == field.FieldId);
                if (existing != null)
                {
                    existing.FieldAddress = field.FieldAddress;
                    existing.Latitude = field.Latitude;
                    existing.Longitude = field.Longitude;
                    _context.SaveChanges();
                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Hřiště uspěšně aktualizováno"
                    };
                }
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Hřiště neuspěšně aktualizováno"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateExistingFieldsAsync] Error updating fields.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Chyba při procesu aktualizace hřišť!"
                };
            }
        }
        public async Task<RepositoryResponse> AddVeto(Veto vetoToAdd)
        {
            try
            {
                var existingRelation = await _context.Vetoes
                                        .Where(v => v.TeamId == vetoToAdd.TeamId && v.CompetitionId == vetoToAdd.CompetitionId && v.RefereeId ==vetoToAdd.RefereeId )
                                        .FirstOrDefaultAsync();
                if (existingRelation != null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Veto s tímto id týmu a id soutěže již existuje!"
                    };
                }
                else
                {
                    _context.Vetoes.Add(vetoToAdd);
                    await _context.SaveChangesAsync();

                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Veto uspěšně pridáno"
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddVeto] Error saving veto");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Došlo k chybě při ukládání veta."
                };

            }
        }
        public async Task<RepositoryResponse> AddTransfer(Transfer transferToAdd)
        {
            try
            {
                var existingRelation = await _context.Transfers
                                        .Where(t => t.PreviousMatchId == transferToAdd.PreviousMatchId && t.FutureMatchId == transferToAdd.FutureMatchId)
                                        .FirstOrDefaultAsync();
                if (existingRelation != null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Transfer s tímto id zápasů již existuje!"
                    };
                }
                else
                {
                    _context.Transfers.Add(transferToAdd);
                    await _context.SaveChangesAsync();

                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Transfer uspěšně pridán"
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddTransfer] Error saving transfer");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Došlo k chybě při ukládání transferu."
                };

            }
        }

        public RepositoryResponse DeleteVeto(int id)
        {
            try
            {
                var veto = _context.Vetoes
                    .FirstOrDefault(v => v.VetoId == id);

                if (veto == null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Veto s id" + id + "nebylo najdeno!"
                    };
                }

                _context.Vetoes.Remove(veto);
                _context.SaveChanges();

                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Veto úspěšne vymazáno!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeleteVeto] Error deleting veto");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Nepodařilo se vymazat veto z databáze!"
                };
            }
        }

        public RepositoryResponse UpdateExistingVeto(int id, string note) {
            try
            {
                var existing = _context.Vetoes.FirstOrDefault(v => v.VetoId == id);
                if (existing != null)
                {
                    existing.Note = note;
                    _context.SaveChanges();
                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Veto uspěšně aktualizováno"
                    };
                }
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Veto neuspěšně aktualizováno"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateExistingVeto] Error updating veto.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Chyba při procesu aktualizace veta!"
                };
            }

        }


        public async Task<RepositoryResult<List<FilesPreviousDelegation>>> GetFilesWithPreviousMatchesAsync()
        {
            try
            {
                var listOfFilesWithPreviousMatches = await _context.FilesPreviousDelegations
                    .OrderBy(r => r.FileUploadedDatetime)
                    .ToListAsync();

                if (listOfFilesWithPreviousMatches != null)
                {
                    return RepositoryResult<List<FilesPreviousDelegation>>.Success(listOfFilesWithPreviousMatches);
                }
                return RepositoryResult<List<FilesPreviousDelegation>>.Failure("Nepodařilo se získat záznamy souborů s odehranými zápasy z databáze (tabuľka neexistuje)");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetFilesWithPreviousMatchesAsync] Error getting files with previous matches");
                return RepositoryResult<List<FilesPreviousDelegation>>.Failure("Nepodařilo se získat záznamy souborů s odehranými zápasy z databáze");
            }
        }
        public RepositoryResponse AddNewFilePreviousDelegation(string filePath, int amountOfMatches, DateOnly? matchesFrom, DateOnly? matchesTo)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Cesta k souboru nebyla nalezena!"
                    };

                var fileName = Path.GetFileName(filePath);
                DateTime timestampChange = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

                var newEntry = new FilesPreviousDelegation
                {
                    AmountOfMatches = amountOfMatches,
                    DelegationsFrom = matchesFrom ?? DateOnly.MinValue,
                    DelegationsTo = matchesTo ?? DateOnly.MaxValue,
                    FileUploadedDatetime = timestampChange,
                    FileName = fileName
                };

                _context.FilesPreviousDelegations.Add(newEntry);

                return new RepositoryResponse
                {
                    Success = true,
                    Message = ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddNewFilePreviousDelegation] Error uploading played matches.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = ""
                };
            }
        }
        public async Task<RepositoryResponse> AddRefereeToTheMatch(int refereeId, string matchId, int role,string user)
        {
            try
            {
                var match = await _context.Matches
                    .FirstOrDefaultAsync(m => m.MatchId == matchId);

                if (match == null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = $"Zápas s id {matchId} nebyl najden."
                    };
                }

                bool roleIsVacant = false;

                switch (role)
                {
                    case 0:
                        roleIsVacant = match.RefereeId == null;
                        break;
                    case 1:
                        roleIsVacant = match.Ar1Id == null;
                        break;
                    case 2:
                        roleIsVacant = match.Ar2Id == null;
                        break;
                    default:
                        return new RepositoryResponse
                        {
                            Success = false,
                            Message = $"Neplatná role rozhodčího: {role}"
                        };
                }

                if (!roleIsVacant)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = $"Role je již obsazena pro zápas {matchId}."
                    };
                }

                // Assign the referee to the specified role
                switch (role)
                {
                    case 0:
                        match.RefereeId = refereeId;
                        break;
                    case 1:
                        match.Ar1Id = refereeId;
                        break;
                    case 2:
                        match.Ar2Id = refereeId;
                        break;
                }

 		DateTime timestampChange = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

		match.LastChanged = timestampChange;
		match.LastChangedBy = user;
                await _context.SaveChangesAsync();

                return new RepositoryResponse
                {
                    Success = true,
                    Message = $"Rozhodčí byl úspěšně přiřazen k zápasu {matchId}."
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddRefereeToTheMatch] Error uploading referee to match.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = ""
                };
            }

        }
        public async Task<RepositoryResponse> RemoveRefereeFromTheMatch(int refereeId, string matchId,string user)
        {
            try
            {
                var match = await _context.Matches
                    .FirstOrDefaultAsync(m => m.MatchId == matchId);

                if (match == null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = $"Zápas s id {matchId} nebyl najden."
                    };
                }
                if (match.RefereeId.HasValue && match.RefereeId.Value == refereeId)
                {
                    match.RefereeId = null;
                }
                else if (match.Ar1Id.HasValue && match.Ar1Id.Value == refereeId)
                {
                    match.Ar1Id = null;
                }
                else if (match.Ar2Id.HasValue && match.Ar2Id == refereeId)
                {
                    match.Ar2Id = null;
                }

		DateTime timestampChange = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 				TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

		match.LastChanged = timestampChange;
		match.LastChangedBy = user;

                await _context.SaveChangesAsync();

                return new RepositoryResponse
                {
                    Success = true,
                    Message = $"Rozhodčí byl úspěšně vyřazen z zápasu {matchId}."
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RemoveRefereeFromTheMatch] Error removing referee from the match.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = ""
                };
            }

        }
        public async Task<RepositoryResponse> RemoveTransfersConnectedTo(int refereeId, string matchId)
        {
            try
            {
                var transfers = await _context.Transfers
                    .Where(t => (t.PreviousMatchId == matchId || t.FutureMatchId == matchId) && t.RefereeId == refereeId)
                    .ToListAsync();

                if (transfers.Count == 0)
                {
                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = $"Žádne transfery nenajdeny."
                    };
                }

                _context.RemoveRange(transfers);
                await _context.SaveChangesAsync();

                return new RepositoryResponse
                {
                    Success = true,
                    Message = $"Transfery úspěšně vymazány od {matchId}."
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RemoveTransfersConnectedTo] Error erasing transfers of match.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Chyba při mazání transferů."
                };
            }
        }


        public async Task<RepositoryResult<List<Field>>> GetFields()
        {
            try
            {
                var listOfFields = await _context.Fields
                    .OrderBy(r => r.FieldName)
                    .ToListAsync();

                if (listOfFields != null)
                {
                    return RepositoryResult<List<Field>>.Success(listOfFields);
                }
                return RepositoryResult<List<Field>>.Failure("Nepodařilo se získat záznamy hřišť z databáze (tabuľka neexistuje)");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetFields] Error getting fields");
                return RepositoryResult<List<Field>>.Failure("Nepodařilo se získat záznamy hřišť z databáze");
            }
        }
        public async Task<RepositoryResult<List<Competition>>> GetCompetitions()
        {
            try
            {
                var listOfCompetitions = await _context.Competitions
                    .OrderBy(r => r.CompetitionName)
                    .ToListAsync();

                if (listOfCompetitions != null)
                {
                    return RepositoryResult<List<Competition>>.Success(listOfCompetitions);
                }
                return RepositoryResult<List<Competition>>.Failure("Nepodařilo se získat soutěže z databáze (tabuľka neexistuje)");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[v] Error getting competitions");
                return RepositoryResult<List<Competition>>.Failure("Nepodařilo se získat soutěže z databáze");
            }
        }


        public async Task<RepositoryResponse> UpdateMatchAsync(Models.Match match)
        {
            try
            {
                var existingMatch = (await GetMatchByIdAsync(match.MatchId)).GetDataOrThrow();
                if (existingMatch == null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Zápas nenajden."
                    };
                }

                _context.Entry(existingMatch).CurrentValues.SetValues(match);
                await _context.SaveChangesAsync();
                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Zápas úspěšne změnen."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateMatchAsync] Error updating match.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Chyba při aktualizaci zápasu."
                };
            }
        }



        public async Task<RepositoryResponse> UpdatePreMatchRelationship(Models.Match existingMatch, string newPreMatchId)
        {
            // Start a transaction to ensure atomicity of changes to multiple related matches
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (existingMatch.PreMatch != null)
                {
                    // Detach current pre-match from this match by clearing its PostMatch reference
                    var currentPreMatch = (await GetMatchByIdAsync(existingMatch.PreMatch)).GetDataOrThrow();
                    currentPreMatch.PostMatch = null;

                    if (!(await UpdateMatchAsync(currentPreMatch)).Success)
                    {
                        return new RepositoryResponse
                        {
                            Success = false,
                            Message = "Chyba při aktualizaci původního pre-zápasu."
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(newPreMatchId))
                {
                    // Fetch and attach the new pre-match
                    var newPreMatch = (await GetMatchByIdAsync(newPreMatchId)).GetDataOrThrow();
                    newPreMatch.PostMatch = existingMatch.MatchId;

                    if ((await UpdateMatchAsync(newPreMatch)).Success)
                    {
                        existingMatch.PreMatch = newPreMatchId;
                    }
                    else
                    {
                        return new RepositoryResponse
                        {
                            Success = false,
                            Message = "Chyba při ukládání nového pre-zápasu."
                        };
                    }
                }
                else
                {
                    // If new ID is null or empty, remove any existing link
                    existingMatch.PreMatch = null;
                }

                // Save the updated main match
                if (!(await UpdateMatchAsync(existingMatch)).Success)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Chyba při ukládání hlavního zápasu."
                    };
                }

                await transaction.CommitAsync();
                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Úspěsne propojení pre zápas!"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[UpdatePostMatchRelationship] Error match controller ");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Chyba při propojování pre zápasu!"
                };
            }
        }

        public async Task<RepositoryResponse> UpdatePostMatchRelationship(Models.Match existingMatch, string newPostMatchId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Clear existing post-match relationship if any
                if (existingMatch.PostMatch != null)
                {
                    var currentPostMatch = (await GetMatchByIdAsync(existingMatch.PostMatch)).GetDataOrThrow();
                    currentPostMatch.PreMatch = null;

                    if (!(await UpdateMatchAsync(currentPostMatch)).Success)
                    {
                        return new RepositoryResponse
                        {
                            Success = false,
                            Message = "Chyba při aktualizaci původního post-zápasu."
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(newPostMatchId))
                {
                    // Link the new post-match back to this match
                    var newPostMatch = (await GetMatchByIdAsync(newPostMatchId)).GetDataOrThrow();
                    newPostMatch.PreMatch = existingMatch.MatchId;

                    if ((await UpdateMatchAsync(newPostMatch)).Success)
                        existingMatch.PostMatch = newPostMatchId;
                    else
                    {
                        return new RepositoryResponse
                        {
                            Success = false,
                            Message = "Chyba při ukládání nového post-zápasu."
                        };
                    }
                        }
                else
                {
                    existingMatch.PostMatch = null;
                }

                if (!(await UpdateMatchAsync(existingMatch)).Success)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Chyba při ukládání hlavního zápasu."
                    };
                }

                await transaction.CommitAsync();
                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Úspěsne propojení post zápas!"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[UpdatePostMatchRelationship] Error match controller ");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Chyba při propojování post zápasu!"
                };
            }
        }

        private static string? GetMatchTimeColor(DateTime matchStart, DateTime saturdayStart, DateTime saturdayNoon, DateTime sundayStart, DateTime sundayNoon, DateTime sundayEnd)
        {
               return matchStart switch
               {
                   var time when time >= saturdayStart && time < saturdayNoon => "yellow",

                   var time when time >= saturdayNoon && time < sundayStart => "darkblue",

                   var time when time >= sundayStart && time < sundayNoon => "darkred",

                   var time when time >= sundayNoon && time <= sundayEnd => "forestgreen",

                   _ => null
               };
        }

    }
}
