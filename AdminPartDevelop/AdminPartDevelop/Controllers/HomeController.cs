using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AdminPartDevelop.Models;
using AdminPartDevelop.Common;
using AdminPartDevelop.Services.RefereeServices;
using AdminPartDevelop.Views.ViewModels;
using AdminPartDevelop.Services.AdminServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Azure;
using Microsoft.Extensions.Caching.Memory;

namespace AdminPartDevelop.Controllers;
[Route("Admin")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly Services.FileParsers.IExcelParser _excelParser;
    private readonly Services.RefereeServices.IRefereeService _refereeService;
    private readonly Services.AdminServices.IAdminService _adminService;


    private readonly Data.IRefereeRepo _refereeRepo;
    private readonly Data.IAdminRepo _adminRepo;


    public HomeController(Data.IRefereeRepo refereeRepo, Data.IAdminRepo adminRepo, Services.FileParsers.IExcelParser excelParser, Services.RefereeServices.IRefereeService refereeService, Services.AdminServices.IAdminService adminService, ILogger<HomeController> logger)
    {
        _logger = logger;
        _excelParser = excelParser;
        _refereeService = refereeService;
        _adminService = adminService;
        _refereeRepo = refereeRepo;
        _adminRepo = adminRepo;
    }
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
	     // Try to get data about user from headers if they exist
            /*var userId = Request.Headers["X-User-Id"].FirstOrDefault();
            var username = Request.Headers["X-User-Name"].FirstOrDefault();
            var userRole = Request.Headers["X-User-Role"].FirstOrDefault();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userRole))
            {
                        return BadRequest("Missing or empty headers.");
            }*/
	    // Pass the username and role to the view
            ViewBag.Username = "rozhodcipraha@gmail.com";
            ViewBag.UserRole = "MainAdmin";
            //load the referees from the database
            var listOfReferees = (await _refereeRepo.GetRefereesAsync()).GetDataOrThrow();
            //extract only ids (keys) name surname (value) for generating buttons
            var dictWithNamesIds = _refereeService.GetRefereeDictionary(listOfReferees).GetDataOrThrow();
            //load the matches from the database
            var listOfMatches = (await _adminRepo.GetMatchesAsync(dictWithNamesIds)).GetDataOrThrow();
            var listOfMatchesSortedByGameTime = _adminService.SortMatches("sortByGameTimeAsc", listOfMatches).GetDataOrThrow();
            //get actuall percentage of delegate matches
            ViewBag.percentageDelegated = _adminService.GetPercentageOfDelegatedMatches(listOfMatches);

            DateOnly firstGameDay = _adminRepo.GetStartGameDate().GetDataOrThrow();
            //get transfers of all referees within this game weekend
            var listOfTransfers = (await _adminRepo.GetTransfersWithinGameWeekend(firstGameDay.ToDateTime(new TimeOnly(0, 1)))).GetDataOrThrow();
            var listOfRefereesWithTimeOptions = (await _refereeService.AddRefereesTimeOptionsAsync(listOfReferees, listOfMatches,listOfTransfers, firstGameDay)).GetDataOrThrow();
            //divide them into the groups by league
            var dictWithSortedReferees = _refereeService.SortRefereesByLeague(listOfRefereesWithTimeOptions).GetDataOrThrow();

            var viewModelReferees = dictWithSortedReferees.Select(kv => new RefereeTabsViewModel
            {
                LeagueId = kv.Key,
                Referees = kv.Value
            }).ToList();

            MainViewModel mainViewModel = new MainViewModel
            {
                Referees = viewModelReferees,
                Matches = listOfMatchesSortedByGameTime
            };


            ViewBag.FirstGameDay = firstGameDay;
            ViewBag.SecondGameDay = firstGameDay.AddDays(1);

            return View(mainViewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Index] Error home controller");
            return StatusCode(500, "Nastala chyba pøi zobrazování hlavného okna.");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
