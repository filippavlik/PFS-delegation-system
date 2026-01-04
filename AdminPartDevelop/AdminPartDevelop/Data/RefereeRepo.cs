using AdminPartDevelop.DTOs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Common;
using Microsoft.EntityFrameworkCore;
using AdminPartDevelop.Services.FileParsers;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using AdminPartDevelop.Views.ViewModels;

namespace AdminPartDevelop.Data
{
    public class RefereeRepo : IRefereeRepo
    {
        private readonly RefereeDbContext _context;
        private readonly ILogger<RefereeRepo> _logger;

        public RefereeRepo(ILogger<RefereeRepo> logger, RefereeDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<RepositoryResponse> AddRefereeAsync(Referee referee)
        {
            try
            {
                var existingRelation =  await _context.Referees
                                        .Where(s => s.Name == referee.Name && s.Surname == referee.Surname)
                                        .FirstOrDefaultAsync();
                if (existingRelation != null)
                {
                    return new RepositoryResponse
                    {
                        Success = false,
                        Message = "Rozhodčí s tímto jménem již existuje!"
                    };
                }
                else
                {                   
                    _context.Referees.Add(referee);
                    await _context.SaveChangesAsync();

                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Rozhodčí byl úspěšně přidán!"
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddRefereeAsync] Error saving referee");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Došlo k chybě při ukládání rozhodčího."
                };

            }
        }
	public async Task<RepositoryResponse> AddManualExcuses(List<Excuse> excuses)
{
    try
    {
        _context.Excuses.AddRange(excuses);
        await _context.SaveChangesAsync();
        return new RepositoryResponse
        {
            Success = true,
            Message = "Omluvy úspěšně přidáne!"
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[AddVeto] Error saving excuses");
        return new RepositoryResponse
        {
            Success = false,
            Message = "Došlo k chybě při ukládání omluv."
        };

    }

}
        public async Task<RepositoryResult<Referee>> GetRefereeByIdAsync(int id)
        {
            try
            {
                var referee = await _context.Referees
                    .Include(r => r.Excuses)
                    .Include(r => r.VehicleSlots)
                    .Include(r => r.ActuallLocations)
                    .Include(r => r.MaximumAmounts)
                    .FirstOrDefaultAsync(r => r.RefereeId == id);

                if (referee == null)
                {
                    return RepositoryResult<Referee>.Failure("Rozhodčí s id "+id+" nebyl najden!");
                }

                return RepositoryResult<Referee>.Success(referee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereeByIdAsync] Error getting referee");
                return RepositoryResult<Referee>.Failure("Nepodařilo se získat rozhodčího z databáze");
            }
        }

        public async Task<RepositoryResult<List<Referee>>> GetRefereesAsync()
        {
            try
            {
                var listOfReferees = await _context.Referees
                    .Include(r => r.VehicleSlots)
                    .Include(r => r.Excuses)
                    .Include(r => r.ActuallLocations)
                    .Include(r => r.MaximumAmounts)
                    .ToListAsync();
                if (listOfReferees != null)
                {
                    return RepositoryResult<List<Referee>>.Success(listOfReferees);
                }
                return RepositoryResult<List<Referee>>.Failure("Nepodařilo se získat rozhodčí z databáze (tabuľka neexistuje)");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereesAsync] Error getting referees");
                return RepositoryResult<List<Referee>>.Failure("Nepodařilo se získat rozhodčí z databáze");
            }
        }
      
        public async Task<RepositoryResult<List<Excuse>>> GetExcusesAsync()
        {
            try
            {
                var oneWeekAgo = DateTime.Now.AddDays(-7);

                var listOfExcuses = await _context.Excuses
                    .Include(r => r.Referee)
                   
                    .Where(r => r.DateTo != null && r.TimeTo != null &&
                    // Convert DateTo and TimeTo to a DateTime for comparison
                    new DateTime(r.DateTo.Year, r.DateTo.Month, r.DateTo.Day,
                              r.TimeTo.Hour, r.TimeTo.Minute, r.TimeTo.Second)
                    < oneWeekAgo)
                    
                    .OrderByDescending(r => r.DatetimeAdded)                   
                    .ToListAsync();

                if (listOfExcuses != null)
                {
                    return RepositoryResult<List<Excuse>>.Success(listOfExcuses);
                }
                return RepositoryResult<List<Excuse>>.Failure("Nepodařilo se získat omluvy z databáze (tabuľka neexistuje)");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetExcusesAsync] Error getting excuses");
                return RepositoryResult<List<Excuse>>.Failure("Nepodařilo se získat omluvy z databáze");
            }
        }
        public async Task<RepositoryResult<ActuallLocation?>> DoesExistActuallLocationBeforeMatch(RefereeWithTimeOptions referee, Models.Match matchToCheck)
        {
            try
            {
                DateTime matchTime = matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime);
                var locationExists = await _context.ActuallLocations.Where(ac => ac.RefereeId == referee.Referee.RefereeId && (matchTime.AddMinutes(-90) <= ac.DateTo.ToDateTime(ac.TimeTo)) && matchTime >= ac.DateFrom.ToDateTime(ac.TimeFrom)).FirstOrDefaultAsync();

                return RepositoryResult<ActuallLocation?>.Success(locationExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DoesExistActuallLocationBeforeMatch] Error finding out if the actuall location before match exists");
                return RepositoryResult<ActuallLocation?>.Failure("Nepodařilo se získat zda rozhodčí má zadanou aktuální polohu před týmhle zápasem!");
            }
        }
        public async Task<RepositoryResult<ActuallLocation?>> DoesExistActuallLocationAfterMatch(RefereeWithTimeOptions referee, Models.Match matchToCheck)
        {
            try
            {
                DateTime matchTime = matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime);
                var locationExists = await _context.ActuallLocations.Where(ac => ac.RefereeId == referee.Referee.RefereeId && (matchTime.AddMinutes(180) >= ac.DateFrom.ToDateTime(ac.TimeFrom)) && matchTime.AddMinutes(90) <= ac.DateTo.ToDateTime(ac.TimeTo)).FirstOrDefaultAsync();

                return RepositoryResult<ActuallLocation?>.Success(locationExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DoesExistActuallLocationAfterMatch] Error finding out if the actuall location after match exists");
                return RepositoryResult<ActuallLocation?>.Failure("Nepodařilo se získat zda rozhodčí má zadanou aktuální polohu po tomhle zápase!");
            }
        }

        public async Task<RepositoryResult<Dictionary<string, int>>> GetRefereeIdsFromFacrIdOrNameAsync(List<FilledMatchDto> listOfMatches)
        {
            try
            {
                var result = new Dictionary<string, int>();


                var facrIds = listOfMatches
                    .SelectMany(m => new[] { m.IdOfReferee, m.IdOfAr1, m.IdOfAr2 })
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                foreach (var facrId in facrIds)
                {
                    if (result.ContainsKey(facrId))
                        continue;

                    var referee = await _context.Referees.FirstOrDefaultAsync(r => r.FacrId == facrId);

                    if (referee == null)
                    {
                        var matchesWithFacrId = listOfMatches.Where(m =>
                            m.IdOfReferee == facrId ||
                            m.IdOfAr1 == facrId ||
                            m.IdOfAr2 == facrId).ToList();

                        foreach (var match in matchesWithFacrId)
                        {
                            string name = null;
                            string surname = null;

                            if (match.IdOfReferee == facrId)
                            {
                                name = match.NameOfReferee;
                                surname = match.SurnameOfReferee;
                            }
                            else if (match.IdOfAr1 == facrId)
                            {
                                name = match.NameOfAr1;
                                surname = match.SurnameOfAr1;
                            }
                            else if (match.IdOfAr2 == facrId)
                            {
                                name = match.NameOfAr2;
                                surname = match.SurnameOfAr2;
                            }

                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(surname))
                            {
                                referee = await _context.Referees.FirstOrDefaultAsync(r =>
                                    r.Name == name && r.Surname == surname);

                                if (referee != null)
                                {
                                    referee.FacrId = facrId;
                                    await _context.SaveChangesAsync();
                                    break; // Break once we found and updated a referee
                                }
                            }
                        }
                    }

                    if (referee != null)
                    {
                        result.Add(facrId, referee.RefereeId);
                    }
                }


                return RepositoryResult<Dictionary<string, int>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereeIdsFromFacrIdOrNameAsync] Error getting ids in order to facr id");
                return RepositoryResult<Dictionary<string, int>>.Failure("Nepodařilo se získat id rozhodčích z databáze na základe facr id nebo jména");

            }


        }
        public async Task<RepositoryResult<Dictionary<int, Tuple<string, string>>>> GetRefereeRealNameAndFacrIdById()
        {
            try
            {
                var referees = await _context.Referees
                    .Select(r => new
                    {
                        r.RefereeId,
                        FullName = r.Name+ " " + r.Surname,
                        r.FacrId
                    })
                    .ToListAsync();

                var result = referees.ToDictionary(
                    r => r.RefereeId,
                    r => Tuple.Create(r.FullName, r.FacrId)
                );

                return RepositoryResult<Dictionary<int, Tuple<string, string>>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetRefereeRealNameAndFacrIdById] Failed to retrieve data");
                return RepositoryResult<Dictionary<int, Tuple<string, string>>>.Failure("Chyba při získávání informací o rozhodčích.");
            }
        }

        public async Task<RepositoryResponse> UpdateRefereeAsync(int id,RefereeAddRequest referee)
        {
            try
            {
                var existing =  await _context.Referees.FirstOrDefaultAsync(r => r.RefereeId == id);
                if (existing != null)
                {
                    existing.Email = referee.Email;
                    existing.Age = referee.Age;
		    existing.Rating = referee.Rating;
                    //New
                    existing.RefereeAddress = referee.Place;
                    existing.Latitude = referee.Latitude;
                    existing.Longitude = referee.Longtitude;
                    existing.FacrId = referee.FacrId;
                    existing.Note = referee.Note;
                    existing.League = referee.League;
                    existing.Ofs = referee.Ofs;
                    existing.CarAvailability = referee.CarAvailability;

                    await _context.SaveChangesAsync();
                    return new RepositoryResponse
                    {
                        Success = true,
                        Message = "Rozhodčí  uspěšně aktualizován"
                    };
                }
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Rozhodčí neuspěšně aktualizován"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateRefereeAsync] Error updating referee.");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Chyba při procesu aktualizace rozhodčího!"
                };
            }
        }

        public async Task<RepositoryResponse> UpdateRefereesAsync(Dictionary<KeyValuePair<string, string>, IRefereeDto> referees)
        {
            try
            {
                foreach (var item in referees)
                {
                    string name = item.Key.Key;
                    string surname = item.Key.Value;
                    var referee = item.Value;

                    // Check if referee exists
                    var existingReferee = await _context.Referees
                        .FirstOrDefaultAsync(r => r.Name == name && r.Surname == surname);

                    if (existingReferee != null)
                    {
                        if (referee is RefereeLevelDto levelDto)
                        {
                            existingReferee.Age = levelDto.Age;
                            existingReferee.League = levelDto.League;
                            existingReferee.Ofs = levelDto.Ofs;
                        }
                        else if(referee is FilledRefereeDto filledDto)
                        {
                            if (!string.IsNullOrWhiteSpace(filledDto.Email) && existingReferee.Email != filledDto.Email)
                            {
        				// Check if another referee already uses the email
        				bool emailInUse = await _context.Referees
            				.AnyAsync(r => r.Email == filledDto.Email &&
                           		(r.Name != name || r.Surname != surname));

        				if (!emailInUse)
        				{
						existingReferee.Email = filledDto.Email;
        				}
                            }
                            if (!string.IsNullOrWhiteSpace(filledDto.FacrId))
                            {
                                existingReferee.FacrId = filledDto.FacrId;
                            }
                            if (!string.IsNullOrEmpty(existingReferee.Note) && existingReferee.Note.Length >= 14)
				{
    					existingReferee.Note = filledDto.PhoneNumber + existingReferee.Note.Substring(14);
				}
			    else
                            {
                                existingReferee.Note = filledDto.PhoneNumber;
                            }

                        }
                           
                        existingReferee.TimestampChange = TimeZoneInfo.ConvertTimeFromUtc
                                    (DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")); //we want to have timestamp for Prague time

                        _context.Referees.Update(existingReferee);
                    }
                    else if (referee is RefereeLevelDto levelDto)
                    {                        
                            var newReferee = new Referee
                            {
                                Name = referee.Name,
                                Surname = referee.Surname,
                                Email = "not parsed yet>>" + referee.Name + referee.Surname,
                                PragueZone = "0",
                                TimestampChange = TimeZoneInfo.ConvertTimeFromUtc
                                (DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")) //we want to have timestamp for Prague time
                            };
                            newReferee.Age = levelDto.Age;
                            newReferee.League = levelDto.League;
                            newReferee.Ofs = levelDto.Ofs;

                            await _context.Referees.AddAsync(newReferee);
                                                                   
                    }
                }

                // Save all changes at once
                await _context.SaveChangesAsync();
                return new RepositoryResponse
                {
                    Success = true,
                    Message = "Rozhodčí byli úspěšně aktualizováni!"
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateRefereesAsync] Error uploading referees");
                return new RepositoryResponse
                {
                    Success = false,
                    Message = "Nastala chyba pri aktualizováni rozhodčích!"
                };
            }


        }

    }

}
