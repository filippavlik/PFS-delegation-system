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

namespace AdminPartDevelop.Controllers
{
    [Route("Admin/Referee")]
    public class RefereeController : Controller
    {
        private readonly ILogger<RefereeController> _logger;
        private readonly Services.FileParsers.IExcelParser _excelParser;
        // private readonly Services.EmailsSender.EmailsToLoginDbSender _emailSender;
        private readonly Services.RefereeServices.IRefereeService _refereeService;
        private readonly Services.AdminServices.IAdminService _adminService;
        private readonly Services.CacheServices.IMatchesCacheService _matchesCacheService;
        private readonly Services.GeocodingServices.IGeocodingService _geocodingService;



        private readonly Microsoft.AspNetCore.SignalR.IHubContext<HubForReendering> _hubContext;
        private const string MatchesCacheKey = "AppMatches";
        private readonly IMemoryCache _memoryCache;


        private readonly Data.IRefereeRepo _refereeRepo;
        private readonly Data.IAdminRepo _adminRepo;
        public RefereeController(Data.IRefereeRepo refereeRepo, Data.IAdminRepo adminRepo,Services.GeocodingServices.IGeocodingService geocodingService,
            Services.FileParsers.IExcelParser excelParser, Services.CacheServices.IMatchesCacheService matchesCacheService,
            Services.RefereeServices.IRefereeService refereeService, Services.AdminServices.IAdminService adminService,
            Microsoft.AspNetCore.SignalR.IHubContext<HubForReendering> hubContext, IMemoryCache memoryCache, ILogger<RefereeController> logger)
        {
            _logger = logger;
            _excelParser = excelParser;
            _geocodingService = geocodingService;
            // _emailSender = emailSender;
            _matchesCacheService = matchesCacheService;
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                List<Referee> existingReferees = (await _refereeRepo.GetRefereesAsync()).GetDataOrThrow();

                var refereeWithSameInfo = existingReferees
                        .FirstOrDefault(e =>
                                (e.Name == request.Name && e.Surname == request.Surname) ||
                                e.Email == request.Email ||
                            (
                                    (!string.IsNullOrEmpty(request.FacrId) && e.FacrId == request.FacrId)
                            )
                               );

                if (refereeWithSameInfo != null)
                {
                    return StatusCode(400, "Rozhodčí se stejným jménem nebo e-mailem,nebo FačrId již existuje");
                }

                var referee = ToReferee(request);
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var emails = new List<string> { request.Email };
                    /*var sendResult = (await _emailSender.AddEmailsToAllowedListAsync(emails)).GetDataOrThrow();
                    if (!sendResult)
                    {
                            _logger.LogWarning("Some or all emails could not be added to the login DB.");
                            return StatusCode(500, "E-mail nebylo možné přidat do přihlašovací databáze.");
                    }*/
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
        [HttpGet("GetAddressesByInput")]
        public async Task<IActionResult> GetAddressesByInput(string query)
        {         
            try
            {
                var result = (await _geocodingService.SuggestAddress(query)).GetDataOrThrow();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suggested addresses");
                return StatusCode(500, new { message = "Došlo k chybě při získavání adres." });
            }
        }
        [HttpPost("SaveExcuse")]
        public async Task<IActionResult> SaveExcuse([FromBody] ExcuseRequest request)
        {
            try
            {
                if (request == null || request.Excuses == null || request.Excuses.Count == 0)
                {
                    return BadRequest(new { message = "Žádné omluvy neboli nahrány!" });
                }

                List<Excuse> excusesToSave = request.Excuses.Select(excuseReq => new Excuse
                {
                    RefereeId = request.RefereeId,
                    DateFrom = excuseReq.DateFrom,
                    TimeFrom = excuseReq.TimeFrom,
                    DateTo = excuseReq.DateTo,
                    TimeTo = excuseReq.TimeTo,
                    Reason = excuseReq.Reason,
                    Note = "Manuálně přidáno v systému " + excuseReq.Note,
                    DatetimeAdded = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")) //we want to have timestamp for Prague time
                }).ToList();

                var result = _refereeRepo.AddManualExcuses(excusesToSave).Result;
                if (result.Success)
                    return Ok(new { message = "Omluvy nahrány úspěšne!" });
                else
                    return StatusCode(500, "Nastala chyba při ukládaní omluv.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SaveExcuse] An unexpected error occurred");
                return StatusCode(500, new { message = "Chyba systému v procese nahrávaní ,prosím kontaktujte administrátora!" });
            }
        }
        [HttpPost("AddRefereeToTheMatch")]
        public async Task<IActionResult> AddRefereeToTheMatch(int refereeId, string matchId, int role, bool force, string user)
        {
            try
            {
                var matchesResult = await _matchesCacheService.GetMatchesFromCacheAsync();

                var listOfMatches = matchesResult.GetDataOrThrow();
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
                        return StatusCode(400, "Rozhodčí " + refereeFromId.Name + refereeFromId.Surname + " má veto na jeden z tímu (zkontrolujte v okně rozhodčího)!");
                    }
                    bool isFree = _refereeService.CheckTimeAvailabilityOfReferee(referee, matchToCheck).GetDataOrThrow();

                    if (!isFree)
                    {
                        return StatusCode(400, "Rozhodčí " + refereeFromId.Name + refereeFromId.Surname + " je v daný čas zápasu nedostupný (zkontrolujte v okně rozhodčího)!");
                    }
                }

                var calculatedTransfers = (await _refereeService.CalculateTransfersWhenAssigningAsync(matchToCheck, referee, force)).GetDataOrThrow();
                if (!calculatedTransfers.IsManageable)
                {
                    return StatusCode(400, calculatedTransfers.Message);
                }

                var resultOfTransaction = await _adminRepo.AddRefereeToTheMatch(refereeId, matchId, role, user);

                if (resultOfTransaction.Success)
                {
                    // New
                    // If these two add fails, it is not that important to have transfers stored

                    int? amountOfMinutesOfHomeTransfer = null;

                    if (calculatedTransfers.PreMatch != null)
                    {
                        await _adminRepo.RemoveOutGoingTransfers(refereeId, calculatedTransfers.PreMatch.PreviousMatchId);
                        await _adminRepo.AddTransfer(calculatedTransfers.PreMatch);
                    }
                    if (calculatedTransfers.PostMatch != null)
                    {
                        await _adminRepo.RemoveInGoingTransfers(refereeId, calculatedTransfers.PostMatch.FutureMatchId);
                        await _adminRepo.AddTransfer(calculatedTransfers.PostMatch);
                    }
                    if (calculatedTransfers.PreMatch == null || calculatedTransfers.PostMatch == null)
                    {
                        Tuple<Transfer?,Transfer?> homeTransfers = (await _refereeService.CalculateHomeTransferWhenAssigningAsync(matchToCheck, referee)).GetDataOrThrow(); //bez premavky , iba delka
                        //Add transfers from home and to home
                        if (calculatedTransfers.PostMatch == null && homeTransfers.Item1!=null)
                        {
                            await _adminRepo.AddTransfer(
                               homeTransfers.Item1);
                        }

                        if (calculatedTransfers.PreMatch == null && homeTransfers.Item2 != null)
                        {
                            await _adminRepo.AddTransfer(
                                homeTransfers.Item2
                            );
                        }
                    }

                    var updatedListOfMatches = (await _matchesCacheService.UploadRefreshedMatchToCacheAsync(matchId)).GetDataOrThrow();
                    if (updatedListOfMatches.Count == 0)
                    {
                        return StatusCode(500, "Nastala chyba při přidávání rozhodčího " + refereeFromId.Name + refereeFromId.Surname + " na zápas. (získavání zápasů z cache)");
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
                var getMatchesConnectedWithTransfer = (await _adminRepo.GetConnectedMatchesByTransfer(refereeId,matchId)).GetDataOrThrow();
                var resultOfTransferTransaction = await _adminRepo.RemoveTransfersConnectedTo(refereeId, matchId);

                if (getMatchesConnectedWithTransfer.Item1 != null || getMatchesConnectedWithTransfer.Item2 != null)
                {
                    var matchesResult = await _matchesCacheService.GetMatchesFromCacheAsync();
                    var listOfMatches = matchesResult.GetDataOrThrow();
                    DateOnly firstGameDay = _adminRepo.GetStartGameDate().GetDataOrThrow();
                    Referee refereeFromId = (await _refereeRepo.GetRefereeByIdAsync(refereeId)).GetDataOrThrow();
                    var listOfTransfers = (await _adminRepo.GetRefereesTransfersAsync(refereeId)).GetDataOrThrow();

                    //gather informations from all sources and fill the referee profile to check time availability
                    RefereeWithTimeOptions referee = (await _refereeService.AddRefereeTimeOptionsAsync(refereeFromId, listOfMatches, listOfTransfers, firstGameDay)).GetDataOrThrow();

                    if (getMatchesConnectedWithTransfer.Item1!=null)
                    {
                        Tuple<Transfer?, Transfer?> preMatchPostTransferHome = (await _refereeService.CalculateHomeTransferWhenAssigningAsync(getMatchesConnectedWithTransfer.Item1, referee)).GetDataOrThrow(); //bez premavky , iba delka
                        if (preMatchPostTransferHome.Item1 != null)
                        {
                            await _adminRepo.AddTransfer(
                               preMatchPostTransferHome.Item1);
                        }
                    }

                    if (getMatchesConnectedWithTransfer.Item2 != null)
                    {
                        Tuple<Transfer?, Transfer?> postMatchPreTransferHome = (await _refereeService.CalculateHomeTransferWhenAssigningAsync(getMatchesConnectedWithTransfer.Item2, referee)).GetDataOrThrow(); //bez premavky , iba delka
                        if (postMatchPreTransferHome.Item2 != null)
                        {
                            await _adminRepo.AddTransfer(
                               postMatchPreTransferHome.Item2);
                        }
                    }
                }

                if (resultOfTransaction.Success)
                {
                    DateOnly firstGameDay = _adminRepo.GetStartGameDate().GetDataOrThrow();
                    // Use cache service to update
                    var updatedMatchesResult = (await _matchesCacheService.UploadRefreshedMatchToCacheAsync(matchId)).GetDataOrThrow();
                    if (updatedMatchesResult.Count == 0)
                    {
                        return StatusCode(500, "Nastala chyba při odoberání rozhodčího na zápas. (získavání zápasů z cache)");
                    }

                    Referee updatedRefereeFromId = (await _refereeRepo.GetRefereeByIdAsync(refereeId)).GetDataOrThrow();
                    var updatedListOfTransfers = (await _adminRepo.GetRefereesTransfersAsync(refereeId)).GetDataOrThrow();


                    RefereeWithTimeOptions updatedReferee = (await _refereeService.AddRefereeTimeOptionsAsync(updatedRefereeFromId, updatedMatchesResult, updatedListOfTransfers, firstGameDay)).GetDataOrThrow();
                    DateTime timestampChangeHub = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

                    await _hubContext.Clients.All.SendAsync("AcceptChangeMatchRemove", matchId, updatedReferee.Referee.RefereeId, user, timestampChangeHub);
                    await _hubContext.Clients.All.SendAsync("AcceptChangeReferee", new
                    {
                        refereeId = refereeId,
                        refereeData = updatedRefereeFromId
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

                var listOfMatches = (await _matchesCacheService.GetMatchesFromCacheAsync()).GetDataOrThrow();
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
        //New
        [HttpPost("UpdateRefereeAsync")]
        public async Task<IActionResult> UpdateRefereeAsync(int id, string name, string surname, string idFacr, string email, int rating, int age, int league, bool car, bool pfs, string place, string note, float latitude, float longtitude)
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
                    Rating = rating,
                    //New
                    Latitude = latitude,
                    Longtitude = longtitude,
                    Ofs = pfs,
                    Note = note,
                    CarAvailability = car,
                    Place = place == null ? "0" : place,
                };
                //TODO only if changed
                /*if (!string.IsNullOrWhiteSpace(email))
                {
                        var emails = new List<string> { email };
                        var sendResult = (await _emailSender.AddEmailsToAllowedListAsync(emails)).GetDataOrThrow();
                        if (!sendResult)
                        {
                                _logger.LogWarning("Some or all emails could not be added to the login DB.");
                                return StatusCode(500, "E-maily nebylo možné přidat do přihlašovací databáze.");
                        }
                }*/

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
                /*var sendResult = (await _emailSender.AddEmailsToAllowedListAsync(emailList)).GetDataOrThrow();
                if (!sendResult)
                {
                        _logger.LogWarning("Some or all emails could not be added to the login DB.");
                        return StatusCode(500, "Některé nebo všechny e-maily nebylo možné přidat do přihlašovací databáze..");
                }*/
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
        
        private static Referee ToReferee(DTOs.RefereeAddRequest request)
        {
            return new Referee
            {
                FacrId = request.FacrId,
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email,
                League = request.League,
                Age = request.Age,
                Rating = 7,
                //New
                Latitude = request.Latitude,
                Longitude = request.Longtitude,
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
