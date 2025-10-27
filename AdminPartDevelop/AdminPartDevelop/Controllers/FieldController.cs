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
    [Route("Admin/Field")]
    public class FieldController : Controller
    {
        private readonly ILogger<FieldController> _logger;
        private readonly Services.FileParsers.IExcelParser _excelParser;

        private readonly Data.IAdminRepo _adminRepo;
        public FieldController(Data.IRefereeRepo refereeRepo, Data.IAdminRepo adminRepo, Services.FileParsers.IExcelParser excelParser,ILogger<FieldController> logger)
        {
            _logger = logger;
            _excelParser = excelParser;
            _adminRepo = adminRepo;
        }
        
	[HttpGet("GetPreviewOfFields")]
        public async Task<IActionResult> GetPreviewOfFields()
        {
            var fields = (await _adminRepo.GetFields()).GetDataOrThrow();
            return PartialView("~/Views/PartialViews/_FieldsTable.cshtml", fields);

        }
	[HttpPost("UploadFieldsInformationsFromFileAsync")]
        public async Task<IActionResult> UploadFieldsInformationsFromFileAsync(IFormFile file)
        {
            try
            {
                var filePath = (await _excelParser.SaveAndValidateFileAsync(file)).GetDataOrThrow();
                var listOfFields = (await _excelParser.GetInformationsAboutFields(filePath)).GetDataOrThrow();

                var reponseOfTransaction = (await _adminRepo.UpdateFieldsAsync(listOfFields));

                if (reponseOfTransaction.Success)
                    return Ok(reponseOfTransaction.Message);
                else
                    return StatusCode(500, reponseOfTransaction.Message);

            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadFieldsInformationsFromFileAsync] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání informací o hřištích na server.");
            }
        }
        [HttpPost("UpdateSingleField")]
        public IActionResult UpdateSingleField([FromForm] int fieldId, string fieldName, string fieldAddress, float latitude, float longitude)
        {
            try
            {
                var fieldToUpdate = new FieldToUpdateDto
                {
                    FieldId = fieldId,
                    FieldName = fieldName,
                    FieldAddress = fieldAddress,
                    Latitude = latitude,
                    Longitude = longitude,
                };

                var responseOfTransaction = _adminRepo.UpdateExistingField(fieldToUpdate);

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
                _logger.LogError(ex, "[UpdateFields] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání informací o hřištích na server.");
            }
        }
        [HttpPost("CreateSingleField")]
        public async  Task<IActionResult> CreateSingleField([FromForm] string newFieldName, string newFieldAddress, float newLatitude, float newLongitude)
        {
            try
            {
                var newField = new Field(newFieldName, newFieldAddress, newLatitude, newLongitude);

                var responseOfTransaction = await _adminRepo.AddField(newField);

                if (responseOfTransaction.Success)
                {
                    return Ok(responseOfTransaction);
                }
                else
                {
                    return StatusCode(500, responseOfTransaction);
                }
            }
            catch (InvalidOperationException inEx)
            {
                return StatusCode(500, inEx.Message);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateFields] Error home controller");
                return StatusCode(500, "Nastala chyba při nahrávání informací o hřištích na server.");      
            }         
        }
       
    }
}
