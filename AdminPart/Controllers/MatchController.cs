using AdminPart.DTOs;
using AdminPart.Hubs;
using AdminPart.Models;
using AdminPart.Services.FileParsers;
using AdminPart.Views.ViewModels;
using Aspose.Cells;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Device.Location;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdminPart.Controllers
{
    [Route("Admin/Match")]
    public class MatchController : Controller
    {
        private readonly ILogger<MatchController> _logger;
        private readonly Services.FileParsers.IExcelParser _excelParser;
        private readonly Services.FileParsers.IExcelExporter _excelExporter;
        private readonly Services.RefereeServices.IRefereeService _refereeService;
        private readonly Services.AdminServices.IAdminService _adminService;

        private readonly Data.IRefereeRepo _refereeRepo;
        private readonly Data.IAdminRepo _adminRepo;

        private readonly IHubContext<HubForReendering> _hubContext;

        public MatchController(Data.IRefereeRepo refereeRepo, Data.IAdminRepo adminRepo, Services.FileParsers.IExcelParser excelParser, Services.FileParsers.IExcelExporter excelExporter, Services.RefereeServices.IRefereeService refereeService, Services.AdminServices.IAdminService adminService, IHubContext<HubForReendering> hubContext, ILogger<MatchController> logger)
        {
            _logger = logger;
            _excelParser = excelParser;
            _excelExporter = excelExporter;
            _refereeService = refereeService;
            _adminService = adminService;
            _refereeRepo = refereeRepo;
            _adminRepo = adminRepo;
            _hubContext = hubContext;
        }
        [HttpPost("GetRefereeMatchCounts")]
        public async Task<IActionResult> GetRefereeMatchCounts([FromBody] RefereeMatchCountViewModel request)
        {
            try
            {
                List<int> refereeIds = request.RefereeIds;
                string teamId = request.TeamId;
                bool isReferee = request.IsReferee;
                string competitionId = request.CompetitionId;

                var result = (await _adminService.GetRefereeMatchStatsAsync(new RefereesTeamsMatchesRequestDto
                {
                    CompetitionId = competitionId,
                    RefereeIds = refereeIds,
                    IsReferee = isReferee,
                    TeamId = teamId
                })).GetDataOrThrow();

                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereeMatchCounts] Error home controller");
                return StatusCode(500, "Nastala chyba pri počítaní zápasů rozhodčím.");
            }
        }
        [HttpGet("GetMatchesByDate")]
        public async Task<IActionResult> GetMatchesByDate(DateOnly startDate, TimeOnly startTime, DateOnly endDate, TimeOnly endTime)
        {
            try
            {
                //load the referees from the database
                var listOfReferees = (await _refereeRepo.GetRefereesAsync()).GetDataOrThrow();
                //extract only ids (keys) name surname (value) for generating buttons
                var dictWithNamesIds = _refereeService.GetRefereeDictionary(listOfReferees).GetDataOrThrow();
                DateTime startDateTime = startDate.ToDateTime(startTime);
                DateTime endDateTime = endDate.ToDateTime(endTime);

                var listOfMatches = (await _adminRepo.GetMatchesByDateAsync(dictWithNamesIds, startDateTime, endDateTime)).GetDataOrThrow();
                var listOfMatchesSortedByGameTime = _adminService.SortMatches("sortByGameTimeAsc", listOfMatches).GetDataOrThrow();

                return PartialView("~/Views/PartialViews/_MatchesTable.cshtml", listOfMatchesSortedByGameTime);
            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetMatchesByDate] Error home controller");
                return StatusCode(500, "Nastala chyba při získavání zápasů dle data z databáze.");
            }
        }
        [HttpGet("GetSortedMatchesAsync")]
        public async Task<IActionResult> GetSortedMatchesAsync(string selector)
        {
            try
            {
                //load the referees from the database
                var listOfReferees = (await _refereeRepo.GetRefereesAsync()).GetDataOrThrow();
                //extract only ids (keys) name surname (value) for generating buttons
                var dictWithNamesIds = _refereeService.GetRefereeDictionary(listOfReferees).GetDataOrThrow();
                var listOfMatches = (await _adminRepo.GetMatchesAsync(dictWithNamesIds)).GetDataOrThrow();
                var listOfMatchesSortedByCriterium = _adminService.SortMatches(selector, listOfMatches).GetDataOrThrow();

                return PartialView("~/Views/PartialViews/_MatchesTable.cshtml", listOfMatchesSortedByCriterium);

            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetSortedMatchesAsync] Error home controller");
                return StatusCode(500, "Nastala chyba při získávaní vytřídeních zápasů z server.");
            }
        }
        [HttpPost("GetRefereesPoints")]
        public async Task<IActionResult> GetRefereesPoints([FromBody] RefereeMatchPointsViewModel request)
        {
            try
            {
                List<int> refereeIds = request.RefereeIds;

                List<Referee> refereeList = refereeIds
                    .Select(id => new Referee { RefereeId = id })
                    .ToList();

                string matchId = request.MatchId;

                var match = (await _adminRepo.GetMatchByIdAsync(matchId)).GetDataOrThrow();


                List<RefereesTeamsMatchesResponseDto> resultHomeTeam = (await _adminService.GetRefereeMatchStatsAsync(new RefereesTeamsMatchesRequestDto
                {
                    RefereeIds = refereeIds,
                    TeamId = request.HomeTeamId,
                    CompetitionId = match.CompetitionId,
                    IsReferee = request.IsReferee
                })).GetDataOrThrow();

                List<RefereesTeamsMatchesResponseDto> resultAwayTeam = (await _adminService.GetRefereeMatchStatsAsync(new RefereesTeamsMatchesRequestDto
                {
                    RefereeIds = refereeIds,
                    TeamId = request.AwayTeamId,
                    CompetitionId = match.CompetitionId,
                    IsReferee = request.IsReferee
                })).GetDataOrThrow();


                Dictionary<int, int> distanceDictionary = new();

                List<Models.Match> listOfMatches = (await _adminRepo.GetPureMatchesAsync()).GetDataOrThrow();
                List<RefereesMatchesResponseDto> resultTotal = refereeIds
                    .Select(id =>
                    {
                        int mainRefCount = listOfMatches.Count(m => m.RefereeId == id);
                        int assistantRefCount = listOfMatches.Count(m => m.Ar1Id == id || m.Ar2Id == id);

                        return new RefereesMatchesResponseDto
                        {
                            RefereeId = id,
                            asReferee = mainRefCount,
                            asAssistantReferee = assistantRefCount
                        };
                    })
                    .ToList();
		const float epsilon = 1e-6f;
		bool matchFieldNotSet = false;
                if (Math.Abs(match.Field.Latitude) < epsilon || Math.Abs(match.Field.Longitude) < epsilon)
                {
                                matchFieldNotSet=true;
		}
            	foreach (var refereeId in refereeIds)
		{
    			if (matchFieldNotSet)
    			{
        			distanceDictionary[refereeId] = 0;
    			}
    			else
    			{
        			var matchesForReferee = listOfMatches
        			.Where(match => match.RefereeId == refereeId)
        			.ToList();

			        var locationBefore = _refereeService.GetLocationBeforeMatch(matchesForReferee, match.MatchDate.ToDateTime(match.MatchTime)).GetDataOrThrow();
        			var locationAfter = _refereeService.GetLocationAfterMatch(matchesForReferee, match.MatchDate.ToDateTime(match.MatchTime)).GetDataOrThrow();

    				distanceDictionary[refereeId] = _adminService.CalculateAverageDistance(locationBefore, locationAfter, match).GetDataOrThrow();
    			}
		}
	    

                var result = _refereeService.CalculatePointsForReferees(refereeIds, resultHomeTeam, resultAwayTeam, resultTotal, distanceDictionary).GetDataOrThrow();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereesPoints] Error home controller");
                return StatusCode(500, "Nastala chyba pri počítaní bodů rozhodčím.");
            }
        }
        [HttpPost("GetRefereeMatches")]
        public async Task<IActionResult> GetRefereeMatches([FromBody] List<Referee> listOfReferees)
        {
            try
            {
                var refereeIds = listOfReferees.Select(r => r.RefereeId).ToHashSet();

                var queryResult = (await _adminRepo.GetPureMatchesAsync()).GetDataOrThrow();

                var result = refereeIds
                    .Select(id =>
                    {
                        int mainRefCount = queryResult.Count(m => m.RefereeId == id);
                        int assistantRefCount = queryResult.Count(m => m.Ar1Id == id || m.Ar2Id == id);

                        return new
                        {
                            RefereeId = id,
                            AsMainReferee = mainRefCount,
                            AsAssistantReferee = assistantRefCount
                        };
                    })
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereeMatches] Error home controller");
                return StatusCode(500, "Nastala chyba pri získavání zápasů rozhodčího.");
            }
        }
        [HttpGet("GetTeamsByInput")]
        public async Task<IActionResult> GetTeamsByInput(string input)

        {
            try
            {
                var queryResult = (await _adminRepo.GetTeamsByInput(input)).GetDataOrThrow();

                return Ok(queryResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetTeamsCompetitions] Error home controller");
                return StatusCode(500, "Nastala chyba pri získavání tímov.");
            }
        }
        [HttpGet("GetCompetitions")]
        public async Task<IActionResult> GetCompetitions()

        {
            try
            {
                var queryResult = (await _adminRepo.GetCompetitions()).GetDataOrThrow();

                return Ok(queryResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetCompetitions] Error home controller");
                return StatusCode(500, "Nastala chyba pri získavání souteží.");
            }
        }
        [HttpGet("GetPreviousMatches")]
        public async Task<IActionResult> GetPreviousMatches()
        {
            var previousMatches = (await _adminRepo.GetFilesWithPreviousMatchesAsync()).GetDataOrThrow();
            return PartialView("~/Views/PartialViews/_PreviousMatchesTable.cshtml", previousMatches);

        }

        [HttpPost("LockOrUnlockMatch")]
        public async Task<IActionResult> LockOrUnlockMatch(string matchId,string user)
        {
            try
            {
                bool isLockedNow = (await _adminRepo.UpdateMatchLockAsync(matchId,user)).GetDataOrThrow();
		DateTime timestampChangeHub = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                await _hubContext.Clients.All.SendAsync("AcceptMatchLockUpdate", matchId, isLockedNow,user,timestampChangeHub);
                return Ok();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LockOrUnlockMatch] Error home controller");
                return StatusCode(500, "Nastala chyba při získávaní stavu zamčení zápasů.");
            }
        }
        [HttpPost("MakeMatchPlayed")]
        public async Task<IActionResult> MakeMatchPlayed(string matchId,string user)
        {
            try
            {
                var result = (await _adminRepo.UpdateMatchPlayedAsync(matchId,user));
                if (result.Success)
                {
                    return Ok("Zápas úspěšně změněn na odehraný!");
                }
                else
                {
                    return StatusCode(500, "Nastala chyba při měnení stavu zápasů na odohraný.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MakeMatchPlayed] Error home controller");
                return StatusCode(500, "Nastala chyba při měnení stavu zápasů na odohraný.");
            }
        }
        [HttpPost("UpdateMatchesPreAPostMatch")]
        public async Task<IActionResult> UpdateMatchesPreAPostMatch(string matchId, string preMatch, string postMatch)
        {
            try
            {
                // Get the main match
                var existingMatch = (await _adminRepo.GetMatchByIdAsync(matchId)).GetDataOrThrow();
                if (existingMatch == null)
                    return StatusCode(500, "Zápas nebyl nalezen.");
                // First Check if the teams exists
                if((!string.IsNullOrWhiteSpace(preMatch) && ! _adminRepo.DoesMatchExists(preMatch).Success) || (!string.IsNullOrWhiteSpace(postMatch) && ! _adminRepo.DoesMatchExists(postMatch).Success))
                {
                    return StatusCode(500, "jeden z zadaných zápasů neexistuje, nic se nezměnilo!");
                }

                var preMatchResult = await _adminRepo.UpdatePreMatchRelationship(existingMatch, preMatch);
                if (!preMatchResult.Success)
                    return StatusCode(500, "Chyba při ukládání pre zápasu.");

                var postMatchResult = await _adminRepo.UpdatePostMatchRelationship(existingMatch, postMatch);
                if (!postMatchResult.Success)
                    return StatusCode(500, "Chyba při ukládání post zápasu. (pre zápas byl nastaven)");


                return Ok("Zápasy úspěšně spojeny!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateMatchesPreAPostMatch] Chyba v controlleru");
                return StatusCode(500, "Nastala chyba při spojování zápasů.");
            }
        }
        [HttpPost("UploadMatchesFromFileAsync")]
        public async Task<IActionResult> UploadMatchesFromFileAsync(IFormFile file,string user)
        {
            try
            {
                var filePath = (await _excelParser.SaveAndValidateFileAsync(file)).GetDataOrThrow();
                var listOfMatches = (await _excelParser.GetMatchesDataAsync(filePath)).GetDataOrThrow();

                var responseOfService = _adminService.ProccessDtosToMatches(listOfMatches,user).GetDataOrThrow();
                var reponseOfAdminTran = (await _adminRepo.AddMatchesAsync(responseOfService));

                if (reponseOfAdminTran.Success)
                    return Ok(reponseOfAdminTran.Message);
                else
                    return StatusCode(500, reponseOfAdminTran.Message);

            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadMatchesFromFileAsync] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání zápasů na server.");
            }
        }
        [HttpPost("UploadPlayedMatchesFromFileAsync")]
        public async Task<IActionResult> UploadPlayedMatchesFromFileAsync(IFormFile file,string user)
        {
            try
            {
                var filePath = (await _excelParser.SaveAndValidateFileAsync(file)).GetDataOrThrow();
                var listOfMatches = (await _excelParser.GetPlayedMatchesDataAsync(filePath)).GetDataOrThrow();

                var dictOfReferees = (await _refereeRepo.GetRefereeIdsFromFacrIdOrNameAsync(listOfMatches)).GetDataOrThrow();
                var reponseOfAdminTran = (await _adminRepo.TieAndUpdateTheMatchesAsync(listOfMatches, dictOfReferees, filePath,user));

                if (reponseOfAdminTran.Success)
                    return Ok(reponseOfAdminTran.Message);
                else
                    return StatusCode(500, reponseOfAdminTran.Message);

            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadPlayedMatchesFromFileAsync] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání odohratých zápasů na server.");
            }
        }
        [HttpPost("UploadTheGameWeekendDate")]
        public IActionResult UploadTheGameWeekendDate([FromForm] DateOnly weekendDate)
        {
            try
            {
                var response = _adminRepo.UploadStartGameDate(weekendDate);
                if (response.Success)
                    TempData["SuccessMessage"] = "Dátum bol úspešne uložený.";
                else
                    TempData["ErrorMessage"] = "Chyba pri ukladaní dátumu: ";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadTheGameWeekendDate] Error home controller");
                TempData["ErrorMessage"] = "Chyba pri ukladaní dátumu: ";
            }

            return RedirectToAction("Index", "Home");
        }
        [HttpPost("MakeConnectionsOfMatches")]
        public async Task<IActionResult> MakeConnectionsOfMatches()
        {
            try
            {
                //load the referees from the database
                var listOfReferees = (await _refereeRepo.GetRefereesAsync()).GetDataOrThrow();
                //extract only ids (keys) name surname (value) for generating buttons
                var dictWithNamesIds = _refereeService.GetRefereeDictionary(listOfReferees).GetDataOrThrow();
                var listOfMatches = (await _adminRepo.GetMatchesAsync(dictWithNamesIds)).GetDataOrThrow();
                var listOfMatchesWithConnections = _adminService.MakeConnectionsOfMatches(listOfMatches).GetDataOrThrow();

                var listOfMatchesOnlyWithConnections = listOfMatchesWithConnections
                                    .Select(m => m.Match)
                                    .ToList();

                var responseOfTransaction = (await _adminRepo.UpdateMatchesAsync(listOfMatchesOnlyWithConnections));

                if (responseOfTransaction.Success)
                {
                    TempData["SuccessMessage"] = "Spájení úspěšně provedeno!";
                    return PartialView("~/Views/PartialViews/_MatchesTable.cshtml", listOfMatchesWithConnections);
                }
                else
                {
                    TempData["ErrorMessage"] = "Chyba pri spájení zápasú.";
                    return StatusCode(500, responseOfTransaction.Message);
                }


            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MakeConnectionsOfMatches] Error home controller");
                return StatusCode(500, "Nastala chyba při spojování podobných zápasů.");
            }
        }
        [HttpPost("DownloadFileWithFilledMatches")]
        public async Task<IActionResult> DownloadFileWithFilledMatches()
        {
            try
            {
                //load the referees from the database
                var listOfReferees = (await _refereeRepo.GetRefereesAsync()).GetDataOrThrow();
                var dictWithNamesIds = _refereeService.GetRefereeDictionary(listOfReferees).GetDataOrThrow();
                var listOfMatches = (await _adminRepo.GetMatchesAsync(dictWithNamesIds)).GetDataOrThrow();
                var listOfMatchesSortedByCriterium = _adminService.SortMatches("sortByGameTimeAsc", listOfMatches).GetDataOrThrow();
                var dictionaryWithRealInfoAboutRefs = (await _refereeRepo.GetRefereeRealNameAndFacrIdById()).GetDataOrThrow();

                var fileContent = _excelExporter.GenerateMatchExcel(listOfMatchesSortedByCriterium, dictionaryWithRealInfoAboutRefs).GetDataOrThrow();

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "matches_"+DateTime.Today.ToString("dd.MM.yyyy") +".xlsx");
            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DownloadFileWithFilledMatches] Error match controller");
                return StatusCode(500, "Nastala chyba při vytváření souboru nebo získávaní vytřídeních zápasů z serverů.");
            }
        }
    }
}
