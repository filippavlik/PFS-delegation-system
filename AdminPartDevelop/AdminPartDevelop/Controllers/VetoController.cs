using AdminPartDevelop.DTOs;
using AdminPartDevelop.Hubs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Services.FileParsers;
using AdminPartDevelop.Views.ViewModels;
using Aspose.Cells;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Device.Location;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdminPartDevelop.Controllers
{
    [Route("Admin/Veto")]
    public class VetoController : Controller
    {
        private readonly ILogger<VetoController> _logger;      
        private readonly Data.IAdminRepo _adminRepo;

        public VetoController(Data.IAdminRepo adminRepo,ILogger<VetoController> logger)
        {
            _logger = logger;          
            _adminRepo = adminRepo;
        }
	[HttpPost("AddVeto")]
        public async Task<IActionResult> AddVeto(int idOfReferee,string idOfTeam, string idOfCompetition,string note)
        {
            try
            {
                var vetoToAdd = new Veto
                {
                    CompetitionId = idOfCompetition,
                    TeamId = idOfTeam,
                    RefereeId = idOfReferee,
                    Note = note
                };
                var responseOfTransaction = await _adminRepo.AddVeto(vetoToAdd);

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
                _logger.LogError(ex, "[AddVeto] Error home controller");
                return StatusCode(500, "Nastala chyba při přidávaní veta na server.");
            }
        }
	[HttpPost("UpdateVeto")]
        public IActionResult UpdateVeto(int id,string note)
        {
            try
            {
                var responseOfTransaction = _adminRepo.UpdateExistingVeto(id,note);

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
                _logger.LogError(ex, "[UpdateVeto] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání informací o vetu na server.");
            }
        }
	[HttpPost("DeleteVeto")]
        public IActionResult DeleteVeto(int id)
        {
            try
            {
                var responseOfTransaction = _adminRepo.DeleteVeto(id);

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
                _logger.LogError(ex, "[DeleteVeto] Error home controller");
                return StatusCode(500, "Nastala chyba při vymazáváni veta z servera.");
            }
        }       
    }
}
