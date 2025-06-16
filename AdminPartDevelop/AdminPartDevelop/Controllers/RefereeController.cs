using AdminPartDevelop.Hubs;
using AdminPartDevelop.Models;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Services.FileParsers;
using AdminPartDevelop.Views.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.AspNet.SignalR;
using AdminPartDevelop.Services.RouteServices;
using AdminPartDevelop.Services.AdminServices;
using System.ComponentModel.DataAnnotations;

namespace AdminPartDevelop.Controllers
{
    [Route("Admin/Referee")]
    public class RefereeController : Controller
    {
        private readonly ILogger<RefereeController> _logger;
        private readonly Services.FileParsers.IExcelParser _excelParser;
        private readonly Services.EmailsSender.IEmailsToLoginDbSender _emailSender;
        private readonly Services.RefereeServices.IRefereeService _refereeService;
        private readonly IAdminService _adminService;
        


        private readonly Microsoft.AspNetCore.SignalR.IHubContext<HubForReendering> _hubContext;
        private const string MatchesCacheKey = "AppMatches";
        private readonly IMemoryCache _memoryCache;


        private readonly Data.IRefereeRepo _refereeRepo;
        private readonly Data.IAdminRepo _adminRepo;
        public RefereeController(Data.IRefereeRepo refereeRepo, Data.IAdminRepo adminRepo,
             Services.FileParsers.IExcelParser excelParser, Services.EmailsSender.IEmailsToLoginDbSender emailSender,
            Services.RefereeServices.IRefereeService refereeService, IAdminService adminService,
            Microsoft.AspNetCore.SignalR.IHubContext<HubForReendering> hubContext, IMemoryCache memoryCache, ILogger<RefereeController> logger)
        {
            _logger = logger;
            _excelParser = excelParser;
            _emailSender = emailSender;
            _refereeService = refereeService;
            _adminService = adminService;
            _refereeRepo = refereeRepo;
            _adminRepo = adminRepo;
            _hubContext = hubContext;
            _memoryCache = memoryCache;
        }

        [HttpPost("AddNewRefereeAsync")]
        public async Task<IActionResult> AddNewRefereeAsync([FromBody] DTOs.RefereeAddRequest request)
        {
            // Check if model is valid based on annotations
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(request, serviceProvider: null, items: null);

            if (!Validator.TryValidateObject(request, context, validationResults, validateAllProperties: true))
            {
                return BadRequest(validationResults);
            }

            try
            {
                var referee = ToReferee(request);
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var emails = new List<string> { request.Email };
                    var sendResult = (await _emailSender.AddEmailsToAllowedListAsync(emails)).GetDataOrThrow();
                    if (!sendResult)
                    {
                        _logger.LogWarning("Some or all emails could not be added to the login DB.");
                        return StatusCode(500, "E-mail nebylo možné přidat do přihlašovací databáze.");
                    }
                }

                var resultOfTransaction = await _refereeRepo.AddRefereeAsync(referee);
                if (resultOfTransaction.Success)
                {
                    return Ok(resultOfTransaction.Message);
                }
                else
                {
                    return StatusCode(500, resultOfTransaction.Message);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new referee");
                return StatusCode(500, new { message = "Došlo k chybě při ukládání rozhodčího." });
            }
        }
        [HttpPost("AddRefereeToTheMatch")]
        public async Task<IActionResult> AddRefereeToTheMatch(int refereeId, string matchId, int role, bool force, string user)
        {
            try
            {
                var listOfMatches = GetMatchesFromCache().Result;
                DateOnly firstGameDay = _adminRepo.GetStartGameDate().GetDataOrThrow();
                Referee refereeFromId = (await _refereeRepo.GetRefereeByIdAsync(refereeId)).GetDataOrThrow();
                var listOfTransfers = (await _adminRepo.GetRefereesTransfersAsync(refereeId)).GetDataOrThrow();

                //gather informations from all sources and fill the referee profile to check time availability
                RefereeWithTimeOptions referee = (await _refereeService.AddRefereeTimeOptionsAsync(refereeFromId, listOfMatches, listOfTransfers, firstGameDay)).GetDataOrThrow();

                Match matchToCheck = (await _adminRepo.GetMatchByIdAsync(matchId)).GetDataOrThrow();

                //check time availability and vetoes of the referee
                if (!force)
                {
                    bool hasVeto = (await _adminRepo.DoesVetoExist(matchId, refereeId)).GetDataOrThrow();

                    if (hasVeto)
                    {
                        return StatusCode(400, "Daný rozhodčí má veto na jeden z tímu (zkontrolujte v okně rozhodčího)!");
                    }
                    bool isFree = _refereeService.CheckTimeAvailabilityOfReferee(referee, matchToCheck).GetDataOrThrow();

                    if (!isFree)
                    {
                        return StatusCode(400, "Daný rozhodčí je v daný čas zápasu nedostupný (zkontrolujte v okně rozhodčího)!");
                    }
                }
                var calculatedTransfers = (await _refereeService.CalculateTransfersWhenAssigningAsync(matchToCheck, referee, force)).GetDataOrThrow();
                if(!calculatedTransfers.IsManageable)
                    return StatusCode(400, calculatedTransfers.Message);

                var resultOfTransaction = await _adminRepo.AddRefereeToTheMatch(refereeId, matchId, role, user);
                //if these two add fails it is not that important to have transfers stored 
                if (calculatedTransfers.PreMatch != null)
                {
                    await _adminRepo.AddTransfer(calculatedTransfers.PreMatch);
                }
                if (calculatedTransfers.PostMatch != null)
                {
                     await _adminRepo.AddTransfer(calculatedTransfers.PostMatch);
                }

                if (resultOfTransaction.Success)
                {
                    var updatedListOfMatches = UploadRefreshedMatchToCache(matchId).Result;
                    if (updatedListOfMatches.Count == 0)
                    {
                        return StatusCode(500, "Nastala chyba při přidávání rozhodčího na zápas. (získavání zápasů z cache)");
                    }

                    Referee updatedRefereeFromId = (await _refereeRepo.GetRefereeByIdAsync(refereeId)).GetDataOrThrow();
                    var updatedListOfTransfers = (await _adminRepo.GetRefereesTransfersAsync(refereeId)).GetDataOrThrow();

                    RefereeWithTimeOptions updatedReferee = (await _refereeService.AddRefereeTimeOptionsAsync(updatedRefereeFromId, updatedListOfMatches, updatedListOfTransfers, firstGameDay)).GetDataOrThrow();
                    DateTime timestampChangeHub = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    await _hubContext.Clients.All.SendAsync("AcceptChangeMatchAdd", matchId, refereeId, updatedRefereeFromId.Name.Substring(0, 1) + ". " + updatedRefereeFromId.Surname, role, user, timestampChangeHub);
                    await _hubContext.Clients.All.SendAsync("AcceptChangeReferee", new
                    {
                        refereeId = refereeId,
                        refereeData = updatedReferee
                    });


                    return Ok(resultOfTransaction.Message);
                }
                else
                {
                    return StatusCode(500, resultOfTransaction.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddRefereeFromTheMatch] Error referee controller");
                return StatusCode(500, "Nastala chyba při přidávání rozhodčího na zápas.");
            }
        }
        [HttpPost("RemoveRefereeFromTheMatch")]
        public async Task<IActionResult> RemoveRefereeFromTheMatch(string matchId, int refereeId, string user)
        {
            try
            {
                var resultOfTransaction = await _adminRepo.RemoveRefereeFromTheMatch(refereeId, matchId, user);
                var resultOfTransferTransaction = await _adminRepo.RemoveTransfersConnectedTo(refereeId, matchId);
                if (resultOfTransaction.Success)
                {
                    DateOnly firstGameDay = _adminRepo.GetStartGameDate().GetDataOrThrow();
                    var updatedListOfMatches = UploadRefreshedMatchToCache(matchId).Result;
                    if (updatedListOfMatches.Count == 0)
                    {
                        return StatusCode(500, "Nastala chyba při přidávání rozhodčího na zápas. (získavání zápasů z cache)");
                    }

                    Referee updatedRefereeFromId = (await _refereeRepo.GetRefereeByIdAsync(refereeId)).GetDataOrThrow();
                    var updatedListOfTransfers = (await _adminRepo.GetRefereesTransfersAsync(refereeId)).GetDataOrThrow();


                    RefereeWithTimeOptions updatedReferee = (await _refereeService.AddRefereeTimeOptionsAsync(updatedRefereeFromId, updatedListOfMatches, updatedListOfTransfers, firstGameDay)).GetDataOrThrow();
                    DateTime timestampChangeHub = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

                    await _hubContext.Clients.All.SendAsync("AcceptChangeMatchRemove", matchId, updatedReferee.Referee.RefereeId, user, timestampChangeHub);
                    await _hubContext.Clients.All.SendAsync("AcceptChangeReferee", new
                    {
                        refereeId = refereeId,
                        refereeData = updatedReferee
                    });

                    return Ok(resultOfTransaction.Message);
                }
                else
                {
                    return StatusCode(500, resultOfTransaction.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RemoveRefereeFromTheMatch] Error referee controller");
                return StatusCode(500, "Nastala chyba při odstraňování rozhodčího z zápasu.");
            }
        }
        [HttpPost("GetCardInfo")]
        public async Task<IActionResult> GetCardInfo(int id)
        {
            try
            {
                Referee referee = (await _refereeRepo.GetRefereeByIdAsync(id)).GetDataOrThrow();
                DateOnly firstGameDay = _adminRepo.GetStartGameDate().GetDataOrThrow();

                var listOfMatches = GetMatchesFromCache().Result;
                var listOfTransfers = (await _adminRepo.GetRefereesTransfersAsync(id)).GetDataOrThrow();
                var refereeWithTimeOptions = (await _refereeService.AddRefereeTimeOptionsAsync(referee, listOfMatches, listOfTransfers, firstGameDay)).GetDataOrThrow();
                ViewBag.FirstGameDay = firstGameDay;

                var vetoesOfReferee = (await _adminRepo.GetRefereesVetoesAsync(id)).GetDataOrThrow();

                RefereeCardViewModel refereeCardViewModel = new RefereeCardViewModel
                {
                    RefereeWTimeOptions = refereeWithTimeOptions,
                    Vetoes = vetoesOfReferee
                };

                return PartialView("~/Views/PartialViews/_RefereeCard.cshtml", refereeCardViewModel);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetCardInfo] Error referee controller");
                return StatusCode(500, "Nastala chyba při zobrazování rozhodčího.");

            }
        }
        [HttpGet("GetExcuses")]
        public async Task<IActionResult> GetExcuses()
        {
            try
            {
                DateOnly firstGameDay = _adminRepo.GetStartGameDate().GetDataOrThrow();
                ViewBag.FirstGameDay = firstGameDay;


                var excuses = (await _refereeRepo.GetExcusesAsync()).GetDataOrThrow();


                return PartialView("~/Views/PartialViews/_ExcusesTable.cshtml", excuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Excuses] Error referee controller");
                return PartialView("~/Views/Shared/_ErrorPartial.cshtml", "Nastala chyba při načítání omluv rozhodčích.");
            }
        }
        [HttpPost("UpdateRefereeAsync")]
        public async Task<IActionResult> UpdateRefereeAsync(int id, string name, string surname, string idFacr, string email, int age, int league, bool car, bool pfs, string place, string note)
        {
            try
            {
                var refereeToUpdate = new RefereeAddRequest
                {
                    FacrId = idFacr,
                    Name = name,
                    Surname = surname,
                    Email = email,
                    League = league,
                    Age = age,
                    Ofs = pfs,
                    Note = note,
                    CarAvailability = car,
                    Place = place == null ? "0" : place,
                };
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emails = new List<string> { email };
                    var sendResult = (await _emailSender.AddEmailsToAllowedListAsync(emails)).GetDataOrThrow();
                    if (!sendResult)
                    {
                        _logger.LogWarning("Some or all emails could not be added to the login DB.");
                        return StatusCode(500, "E-maily nebylo možné přidat do přihlašovací databáze.");
                    }
                }

                var responseOfTransaction = await _refereeRepo.UpdateRefereeAsync(id, refereeToUpdate);

                if (responseOfTransaction.Success)
                {
                    return Ok(responseOfTransaction.Message);
                }
                else
                {
                    return StatusCode(500, responseOfTransaction.Message);
                }
            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateRefereeAsync] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání informací o rozhodčím na server.");
            }

        }
        [HttpPost("UploadRefereesFromFileAsync")]
        public async Task<IActionResult> UploadRefereesFromFileAsync(IFormFile file)
        {
            try
            {
                var filePath = (await _excelParser.SaveAndValidateFileAsync(file)).GetDataOrThrow();
                var dictOfReferees = (await _excelParser.GetRefereesDataAsync(filePath)).GetDataOrThrow();

                var resultOfTransaction = (await _refereeRepo.UpdateRefereesAsync(dictOfReferees));

                if (resultOfTransaction.Success)
                    return Ok(resultOfTransaction.Message);
                else
                    return StatusCode(500, resultOfTransaction.Message);

            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadRefereesFromFileAsync] Error referee controller");
                return StatusCode(500, "Nastala chyba při nahrávání rozhodčích na server.");
            }
        }
        [HttpPost("UploadRefereesFromEmailFileAsync")]
        public async Task<IActionResult> UploadRefereesFromEmailFileAsync(IFormFile file)
        {
            try
            {
                var filePath = (await _excelParser.SaveAndValidateFileAsync(file)).GetDataOrThrow();
                var dictOfReferees = (await _excelParser.GetInformationsOfReferees(filePath)).GetDataOrThrow();

                // Get email list from referee dictionary
                var emailList = dictOfReferees
                        .Values
                        .OfType<FilledRefereeDto>() // filter only actual FilledRefereeDto instances
                        .Select(r => r.Email?.Trim())
                        .Where(email => !string.IsNullOrWhiteSpace(email))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                // Send emails to login container
                var sendResult = (await _emailSender.AddEmailsToAllowedListAsync(emailList)).GetDataOrThrow();
                if (!sendResult)
                {
                    _logger.LogWarning("Some or all emails could not be added to the login DB.");
                    return StatusCode(500, "Některé nebo všechny e-maily nebylo možné přidat do přihlašovací databáze..");
                }
                var resultOfTransaction = (await _refereeRepo.UpdateRefereesAsync(dictOfReferees));

                if (resultOfTransaction.Success)
                    return Ok(resultOfTransaction.Message);
                else
                    return StatusCode(500, resultOfTransaction.Message);

            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadRefereesFromEmailFileAsync] Error referee controller");
                return StatusCode(500, "Nastala chyba při nahrávání informacích o rozhodčích na server.");
            }
        }
        [HttpPost("UploadRefreshedMatch")]
        public async Task<IActionResult> UploadRefreshedMatch(string matchId)
        {
            try
            {
                var matches = await UploadRefreshedMatchToCache(matchId);

                if (matches.Count == 0)
                    return StatusCode(500, "Nastala chyba při update zápasu do cache.");
                else
                    return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadRefreshedMatch] Error referee controller");
                return StatusCode(500, "Nastala chyba při update zápasu do cache.(neznáma chyba)");
            }
        }

        private async Task<List<Match>> GetMatchesFromCache()
        {
            try
            {
                if (!_memoryCache.TryGetValue(MatchesCacheKey, out List<Match> updatedlistOfMatches))
                {
                    updatedlistOfMatches = (await _adminRepo.GetPureMatchesAsync()).GetDataOrThrow();
                    _memoryCache.Set(MatchesCacheKey, updatedlistOfMatches, TimeSpan.FromHours(1));
                    return updatedlistOfMatches;
                }
                else
                {
                    return updatedlistOfMatches;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetMatchesFromCache] Error referee controller");
                return new List<Match>();
            }
        }
        private async Task<List<Match>> UploadRefreshedMatchToCache(string matchId)
        {
            try
            {
                if (!_memoryCache.TryGetValue(MatchesCacheKey, out List<Match> updatedlistOfMatches))
                {
                    updatedlistOfMatches = (await _adminRepo.GetPureMatchesAsync()).GetDataOrThrow();
                    _memoryCache.Set(MatchesCacheKey, updatedlistOfMatches, TimeSpan.FromHours(1));
                    return updatedlistOfMatches;
                }
                else
                {
                    var matchIndex = updatedlistOfMatches.FindIndex(m => m.MatchId == matchId);
                    if (matchIndex != -1)
                    {
                        var freshMatchData = (await _adminRepo.GetMatchByIdAsync(matchId)).GetDataOrThrow();

                        updatedlistOfMatches[matchIndex] = freshMatchData;

                        _memoryCache.Set(MatchesCacheKey, updatedlistOfMatches, TimeSpan.FromHours(1));
                    }
                    return updatedlistOfMatches;

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadRefreshedMatchToCache] Error referee controller");
                return new List<Match>();
            }
        }
        private static Referee ToReferee( DTOs.RefereeAddRequest request)
        {
            return new Referee
            {
                FacrId = request.FacrId,
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email,
                League = request.League,
                Age = request.Age,
                Ofs = request.Ofs,
                Note = request.Note,
                CarAvailability = request.CarAvailability,
                PragueZone = request.Place ?? "0",
                TimestampChange = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"))
            };
        }
    }
}
