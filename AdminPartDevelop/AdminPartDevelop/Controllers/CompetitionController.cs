using AdminPartDevelop.DTOs;
using AdminPartDevelop.Hubs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Services.FileParsers;
using AdminPartDevelop.Views.ViewModels;
using Aspose.Cells;
using Azure.Core;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Device.Location;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdminPartDevelop.Controllers
{
    [Route("Admin/Competition")]
    public class CompetitionController : Controller
    {
        private readonly ILogger<CompetitionController> _logger;

        private readonly Data.IAdminRepo _adminRepo;
        public CompetitionController(Data.IAdminRepo adminRepo,ILogger<CompetitionController> logger)
        {
            _logger = logger;
            _adminRepo = adminRepo;
        }
        
	[HttpGet("GetPreviewOfCompetitions")]
    public async Task<IActionResult> GetPreviewOfCompetitions()
    {
            var competitions = (await _adminRepo.GetCompetitions()).GetDataOrThrow();
            return PartialView("~/Views/PartialViews/_CompetitionsTable.cshtml", competitions);

    }
       
    
    [HttpPost("UpdateSingleCompetition")]
    public IActionResult UpdateSingleCompetition([FromForm] string id, string name,int length,int amountOfReferees, int league)
        {
            try
            {
                var competitionToUpdate = new CompetitionToUpdateDto
                {
                    CompetitionId = id,
                    CompetitionName = name,
                    CompetitionLength = length,
                    CompetitionAmountOfReferees = amountOfReferees,
                    CompetitionLeague = league
                };

                var responseOfTransaction = _adminRepo.UpdateExistingCompetition(competitionToUpdate);

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
                _logger.LogError(ex, "[UpdateCompetition] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání informací o soutěžích na server.");
            }
        }

    [HttpPost("CreateSingleCompetition")]
    public async Task<IActionResult> CreateSingleCompetition([FromForm] string newId, string newName, int newLength,int newAmountOfReferees, int newLeague)
        {
            try
            {
                var competitionToAdd = new Competition
                {
                    CompetitionId = newId ,
                    CompetitionName= newName,
                    MatchLength = newLength,
                    AmountOfReferees = newAmountOfReferees,
                    League = newLeague
                };
                var responseOfTransaction = await _adminRepo.AddCompetition(competitionToAdd);

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
                _logger.LogError(ex, "[CreateSingleCompetition] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání informací o soutěžích na server.");
            }
        }

    }
}
