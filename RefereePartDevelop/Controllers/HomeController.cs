using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefereePart.Models;
using RefereePart.Models.ViewModels;
using System.Text.Json;

namespace RefereePart.Controllers;

/// <summary>
/// Defines routes for user actions in referee docker container.
/// </summary>
[Route("Referee")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly RefereeDbContext _context;

    public HomeController(ILogger<HomeController> logger, RefereeDbContext context)
    {
        _logger = logger;
        _context = context;

    }

    /// <summary>
    /// Displays the main view while extracting user-related information from request headers.
    /// </summary>
    /// <returns>
    /// Returns the main view populated with user details.
    /// </returns>
    /// <remarks>
    /// This method retrieves user-related data from custom request headers:
    /// <list type="bullet">
    ///     <item><description><c>X-User-Id</c>: The unique identifier of the user,uses in authorization who submit the vehicle availability or excuse.</description></item>
    ///     <item><description><c>X-User-Name</c>: The name of the user.</description></item>
    ///     <item><description><c>X-User-Role</c>: The role assigned to the user.</description></item>
    /// </list>
    /// </remarks>
    /// <response code="200">Returns the main view with user details.</response>
    /// <response code="400">Returns the error message to the user (headers).</response>
    /// <response code="500">Returns the error message to the user (unexpected error).</response>
    [HttpGet("")]
    public IActionResult Index()
    {
    	try
	{
            // Try to get data about user from headers if they exist
            var userId = "24";//Request.Headers["X-User-Id"].FirstOrDefault();
            var username = "filorinytesteriny@gmail.com";//Request.Headers["X-User-Name"].FirstOrDefault();
            var userRole = "Rozhodci";//Request.Headers["X-User-Role"].FirstOrDefault();

    		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userRole))
    		{
        		return BadRequest("Missing or empty headers.");
    		}

    		var model = new UserViewModel
    		{
        		UserId = userId,
        		Username = username,
        		Role = userRole
    		};

		// Trim username for comparison (username is in this case email)
        	var trimmedUsername = username.Trim();
        
        	// Search for a referee with the same email as username (both trimmed)
        	var referee = _context.Referees.FirstOrDefault(r => r.Email.Trim() == trimmedUsername);
        
        	// If referee found and UserId doesn't match, update it
        	if (referee != null && referee.UserId != userId)
        	{
           	 	referee.UserId = userId;
            		referee.TimestampChange = DateTime.Now;
            		_context.SaveChanges();
        	}
    		return View(model);
	}
	catch (KeyNotFoundException keyEx)
	{
		_logger.LogError(keyEx, "[Index] Key not found when accessing headers");
    		return BadRequest("Chyba systému prosím kontaktujte administrátora!(headers)");
	}
	catch (ArgumentNullException argEx)
	{
		_logger.LogError(argEx, "[Index] Null argument when accessing headers");
    		return BadRequest("Chyba systému prosím kontaktujte administrátora!(headers)");
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "[Index] An unexpected error occurred");
    		return StatusCode(500, new { message = "Chyba systému prosím kontaktujte administrátora!"});
	}
    }


    [HttpGet("error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Retrieves the excuses for a specific referee based on the userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user (referee)</param>
    /// <remarks>
    /// Sample request:
    ///     GET /api/referee/GetExcuses?userId=someUserId
    /// </remarks>
    /// <returns>Partial view containing the list of excuses for the referee</returns>
    /// <response code="200">Returns the list of excuses for the referee</response>
    /// <response code="401">Unauthorized access, no referee found for the provided userId</response>
    /// <response code="500">An unexpected error occurred during the process of retrieving excuses</response>
    [HttpGet("GetExcuses")]
    public async Task<IActionResult> GetExcuses(string userId)
    {
	try                                  
	{
		int? refereeId = await GetRefereeIdByUserIdAsync(userId);                                                       	if (!refereeId.HasValue)                                                                                                {                                                                                                                               throw new UnauthorizedAccessException("");                
		}

		var excuses = await _context.Excuses
            		.Where(e => e.RefereeId == refereeId)
			.OrderByDescending(e => e.DateFrom)
    			.ThenByDescending(e => e.TimeFrom)
            		.ToListAsync();

		return PartialView("PartialViews/_ExcusesTablePartial", excuses);		
	}
		
        catch (UnauthorizedAccessException unEx)
        {
		_logger.LogError(unEx, "[GetExcuses] The referee does not exist for this userID");
                return StatusCode(401, new { message = "Neexistuje rozhodca naviazaný na tento email , kontaktujte administrátora."});
        }
        catch (Exception ex)
        {
		_logger.LogError(ex, "[GetExcuses] An unexpected error occurred");
                return StatusCode(500, new { message = "Chyba systému v procese získavání omluv ,prosím kontaktujte administrátora!" });
	}

    }

    /// <summary>
    /// Retrieves the vehicle slots for a specific referee based on the userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user (referee)</param>
    /// <remarks>
    /// Sample request:
    ///     GET /api/referee/GetVehicleSlots?userId=someUserId
    /// </remarks>
    /// <returns>Partial view containing the list of vehicle slots for the referee</returns>
    /// <response code="200">Returns the list of vehicle slots for the referee</response>
    /// <response code="401">Unauthorized access, no referee found for the provided userId</response>
    /// <response code="500">An unexpected error occurred during the process of retrieving vehicle slots</response>
    [HttpGet("GetVehicleSlots")]
    public async Task<IActionResult> GetVehicleSlots(string userId)
    {
        try
        {
		int? refereeId = await GetRefereeIdByUserIdAsync(userId);
        	if (!refereeId.HasValue)
        	{
                	throw new UnauthorizedAccessException("");
                }

                var vehicleSlots = await _context.VehicleSlots
                        .Where(e => e.RefereeId == refereeId)
                        .OrderByDescending(e => e.DateFrom)
                        .ThenByDescending(e => e.TimeFrom)
                        .ToListAsync();

                return PartialView("PartialViews/_VehicleSlotsTablePartial", vehicleSlots);
        }

        catch (UnauthorizedAccessException unEx)
        {
		_logger.LogError(unEx, "[GetVehicleSlots] The referee does not exist for this userID");         
		return StatusCode(401, new { message = "Neexistuje rozhodca naviazaný na tento email , kontaktujte administrátora."});
        }
        catch (Exception ex)
        {
		 _logger.LogError(ex, "[GetVehicleSlots] An unexpected error occurred");                                                      return StatusCode(500, new { message = "Chyba systému v procese získavání omluv ,prosím kontaktujte administrátora!" });
        }

    }

    /// <summary>
    /// Saves a list of excuses for a specific referee.
    /// </summary>
    /// <param name="request">The request object containing the userId and list of excuses to be saved</param>
    /// <returns>A status message indicating whether the excuses were successfully saved or if there was an error</returns>
    /// <response code="200">The excuses were successfully saved</response>
    /// <response code="400">No excuses were uploaded or the request body was invalid</response>
    /// <response code="401">Unauthorized access, no referee found for the provided userId</response>
    /// <response code="500">An unexpected error occurred during the process of saving excuses</response>
    [HttpPost("SaveExcuse")]
    public async Task<IActionResult> SaveExcuse([FromBody] ExcuseRequest request)
    {
	try{
	        if (request == null || request.Excuses == null || request.Excuses.Count == 0)
        	{
            		return BadRequest(new { message ="Žádné omluvy neboli nahrány!" });
        	}

                int? refereeId = await GetRefereeIdByUserIdAsync(request.UserId);
                if (!refereeId.HasValue)
                {
                        throw new UnauthorizedAccessException("");
                }
		List<Excuse> excusesToSave = request.Excuses.Select(excuseReq => new Excuse
                                {
					RefereeId = refereeId.Value,
                                        DateFrom = excuseReq.DateFrom,
                                        TimeFrom = excuseReq.TimeFrom,
                                        DateTo = excuseReq.DateTo,
                                        TimeTo = excuseReq.TimeTo,
                                        Reason = excuseReq.Reason,
                                        Note = excuseReq.Note,
                                	DatetimeAdded = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")) //we want to have timestamp for Prague time
				}).ToList();

	                _context.Excuses.AddRange(excusesToSave);
       	                await _context.SaveChangesAsync();
	
        	        return Ok(new { message = "Omluvy nahrány úspěšne!" });
                }
        catch (UnauthorizedAccessException unEx)
        {
		_logger.LogError(unEx, "[SaveExcuse] The referee does not exist for this userID");
        	return StatusCode(401, new { message = "Neexistuje rozhodca naviazaný na tento email , kontaktujte administrátora."});
	}
        catch (Exception ex)
        {
		 _logger.LogError(ex, "[SaveExcuse] An unexpected error occurred");                                                      return StatusCode(500, new { message = "Chyba systému v procese nahrávaní ,prosím kontaktujte administrátora!" });	
	}    
    }

    /// <summary>
    /// Saves the vehicle availability slots for a specific referee.
    /// </summary>
    /// <param name="request">The request object containing the userId and a list of vehicle slots to be saved</param>
    /// <returns>Status message indicating whether the vehicle availability was successfully saved or if there was an error</returns>
    /// <response code="200">The vehicle availability was successfully saved</response>
    /// <response code="400">No vehicle slots were uploaded or the request body was invalid</response>
    /// <response code="401">Unauthorized access, no referee found for the provided userId</response>
    /// <response code="500">An unexpected error occurred during the process of saving vehicle availability</response>
    [HttpPost("SaveVehicleAvailability")]
    public async Task<IActionResult> SaveVehicleAvailability([FromBody] VehicleSlotRequest request)
    {
    	try{
	        if (request == null || request.VehicleSlots == null || request.VehicleSlots.Count == 0)
        	{
            		return BadRequest(new { message ="Žádné možnosti vozidla neboli nahrány!" });
        	}

                int? refereeId = await GetRefereeIdByUserIdAsync(request.UserId);
                if (!refereeId.HasValue)
                {
                        throw new UnauthorizedAccessException("");
                }
		List<VehicleSlot> slotsToSave = request.VehicleSlots.Select(excuseReq => new VehicleSlot
					{
						RefereeId = refereeId.Value,
						DateFrom = excuseReq.DateFrom,
						TimeFrom = excuseReq.TimeFrom,
						DateTo = excuseReq.DateTo,
						TimeTo = excuseReq.TimeTo,

						HasCarInTheSlot = excuseReq.HasCarInTheSlot,
						DatetimeAdded = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")) //we want to have timestamp for Prague time
					}).ToList();

		_context.VehicleSlots.AddRange(slotsToSave);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Možnosti vozidla nahrány úspěšne!" });
	}
	catch (UnauthorizedAccessException unEx)
	{
		 _logger.LogError(unEx, "[SaveVehicleSlot] The referee does not exist for this userID");                                 return StatusCode(401, new { message = "Neexistuje rozhodca naviazaný na tento email , kontaktujte administrátora."});
        }
        catch (Exception ex)
        {
		 _logger.LogError(ex, "[SaveVehicleSlot] An unexpected error occurred");                                                 return StatusCode(500, new { message = "Chyba systému v procese nahrávaní ,prosím kontaktujte administrátora!" });
        }
    }

   /// <summary>
   /// Removes a specific excuse by its unique ID.
   /// </summary>
   /// <param name="excuseId">The unique identifier of the excuse to be removed</param>
   /// <remarks>
   /// Sample request:
   ///     POST /api/referee/RemoveExcuse?excuseId=123
   /// </remarks>
   /// <returns>Status message indicating whether the excuse was successfully removed or if there was an error</returns>
   /// <response code="200">The excuse was successfully removed</response>
   /// <response code="404">The excuse with the provided ID was not found</response>
   /// <response code="500">An unexpected error occurred while removing the excuse</response>    
   [HttpPost("RemoveExcuse")]
   public async Task<IActionResult> RemoveExcuse(int excuseId){
	   try{
		var excuseToRemove = await _context.Excuses
			.FirstOrDefaultAsync(e => e.ExcuseId == excuseId);
		if (excuseToRemove == null)
    		{
       			return NotFound($"Omluva s id {excuseId} se nenašla.");
    		}	

		_context.Excuses.Remove(excuseToRemove);
		await _context.SaveChangesAsync();

		return Ok($"Omluva s {excuseId} byla úspěšne odstránena.");

	   }catch(Exception ex)
	   {
		_logger.LogError(ex, "[RemoveExcuse] An unexpected error occurred");                                                    return StatusCode(500, new { message = "Chyba systému v procese mazání ,prosím kontaktujte administrátora!" });
	   }	
   }

   /// <summary>                                                                                                           /// Removes a specific vehicle slot by its unique ID.                                                                   /// </summary>                                                                                                          /// <param name="vehicleSlotId">The unique identifier of the vehicle slot to be removed</param>                         /// <remarks>                                                                                                           /// Sample request:                                                                                                     ///     POST /api/referee/RemoveVehicleSlot?vehicleSlotId=123                                                           /// </remarks>                                                                                                          /// <returns>Status message indicating whether the vehicle slot was successfully removed or if there was an error</returns> 
   /// <response code="200">The vehicle slot was successfully removed</response>
   /// <response code="404">The vehicle slot with the provided ID was not found</response>
   /// <response code="500">An unexpected error occurred while removing the vehicle slot</response>
   [HttpPost("RemoveVehicleSlot")]
        public async Task<IActionResult> RemoveVehicleSlot(int vehicleSlotId){
		try{
                	var vehicleSlotToRemove = await _context.VehicleSlots
                                        .FirstOrDefaultAsync(e => e.SlotId == vehicleSlotId);

                	if (vehicleSlotToRemove == null)
                	{
                        	return NotFound($"Tato možnost vozidla s id {vehicleSlotId} se nenašla.");
                	}

                	_context.VehicleSlots.Remove(vehicleSlotToRemove);
                	await _context.SaveChangesAsync();

                	return Ok($"Možnost vozidla s {vehicleSlotId} byla úspěšne odstránena.");
        	}catch(Exception ex)
		{
			_logger.LogError(ex, "[RemoveVehicleSlot] An unexpected error occurred");                                                 return StatusCode(500, new { message = "Chyba systému v procese mazání ,prosím kontaktujte administrátora!" });
		}
	}

   	/// <summary>
	/// Logs out the user by deleting the authentication token cookie,redirecting is on the frontend side.
	/// </summary>
	/// <remarks>
	/// Sample request:
	///     POST /api/auth/Logout
	/// </remarks>
	/// <returns>Status message indicating successful logout</returns>
	/// <response code="200">The user was successfully logged out</response>
	/// <response code="500">Unknown error or no authentication token found in cookies</response>
	[HttpPost("Logout")]
	public IActionResult Logout()
	{
		try{
    			if (Request.Cookies.ContainsKey("auth_token"))
    			{
        			Response.Cookies.Delete("auth_token");
    			}

    			return Ok(new { message = "Úspešne odhlásený!" });
		}catch(Exception ex)                                                                                                    {                                                                                                                               _logger.LogError(ex, "[Logout] An unexpected error occurred");                                                 		return StatusCode(500, new { message = "Chyba systému v procese odhlašování ,prosím kontaktujte administrátora!" });
                }
	}
	
        private async Task<int?> GetRefereeIdByUserIdAsync(string userId)
        {
		var referee = await _context.Referees
        		.Where(r => r.UserId == userId)
        		.FirstOrDefaultAsync();
                
		return referee?.RefereeId; // If found, return RefereeId, otherwise return null
        }


}
