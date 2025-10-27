using AdminPartDevelop.Common;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Services.FileParsers;
using AdminPartDevelop.Services.RouteServices;
using AdminPartDevelop.Views.ViewModels;
using Aspose.Cells.Drawing.Equations;
using Microsoft.Maui.ApplicationModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static AdminPartDevelop.Services.RefereeServices.RefereeService;
using static AdminPartDevelop.Views.ViewModels.RefereeWithTimeOptions;

namespace AdminPartDevelop.Services.RefereeServices
{
    public class RefereeService : IRefereeService
    {
        private readonly ILogger<RefereeService> _logger;
	private readonly Data.IAdminRepo _adminRepo;
        private readonly Services.RouteServices.IRouteBusPlanner _routeBusPlanner;
        private readonly Services.RouteServices.IRouteCarPlanner _routeCarPlanner;

        private readonly int waitingTimeBeforeMatch = 45;
        private readonly int reserveAfterMatches = 30;
        private readonly int travellingMaximumPeriod = 90;
	private readonly int locationToBeRelevantTimeGap = 4;

        public RefereeService(Data.IAdminRepo adminRepo, Services.RouteServices.IRouteCarPlanner routeCarPlanner, Services.RouteServices.IRouteBusPlanner routeBusPlanner,ILogger<RefereeService> logger, Data.IRefereeRepo refereeRepo)
        {
	    _adminRepo = adminRepo;
	    _routeCarPlanner = routeCarPlanner;
            _routeBusPlanner = routeBusPlanner;
            _logger = logger;
        }

        public ServiceResult<Dictionary<int, List<RefereeWithTimeOptions>>> SortRefereesByLeague(List<RefereeWithTimeOptions> listOfReferees)
        {
            try
            {
                // Prepares 6 buckets (for leagues 0–5) to group referees
                var refereesByLevel = Enumerable.Range(0, 6)
                    .ToDictionary(level => level, level => new List<RefereeWithTimeOptions>());
                foreach (var referee in listOfReferees)
                {
                    if (referee.Referee.League >= 0 && referee.Referee.League <= 5)
                    {
                        refereesByLevel[referee.Referee.League].Add(referee);
                    }
                    else
                    {
                        return ServiceResult<Dictionary<int, List<RefereeWithTimeOptions>>>.Failure("Rozhodčí s jménem:" + referee.Referee.Name + " " + referee.Referee.Surname + " nemá přiradzenou ligu");
                    }
                }

                //sort the single lists by referees surname
                foreach (var level in refereesByLevel.Keys.ToList())
                {
                    refereesByLevel[level] = refereesByLevel[level]
                        .OrderBy(referee => referee.Referee.Surname)
                        .ThenBy(referee => referee.Referee.Name)
                        .ToList();
                }

                return ServiceResult<Dictionary<int, List<RefereeWithTimeOptions>>>.Success(refereesByLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SortRefereesByLeague] Error sorting referees");
                return ServiceResult<Dictionary<int, List<RefereeWithTimeOptions>>>.Failure("Nepodařilo se vytřídit rozhodčí dle soutěže!");

            }


        }
        public async Task<ServiceResult<RefereeWithTimeOptions>> AddRefereeTimeOptionsAsync(Referee referee, List<Models.Match> listOfMatches, List<Models.Transfer> listOfTransfers, DateOnly firstGameDay)
        {
            try
            {
                RefereeWithTimeOptions refereeWithTimeOptions = new RefereeWithTimeOptions(referee);

                // Define key time points for the weekend schedule:
                // - Saturday early morning (00:01)
                // - Saturday noon (12:00)
                // - Sunday early morning (00:01)
                // - Sunday noon (12:00)
                // - Sunday end (23:59)
                // These time points are used to categorize referee availability into time slots
                var saturdayStart = firstGameDay.ToDateTime(new TimeOnly(0, 1));
                var saturdayNoon = firstGameDay.ToDateTime(new TimeOnly(12, 0));

                var sunday = firstGameDay.AddDays(1);
                var sundayStart = sunday.ToDateTime(new TimeOnly(0, 1));
                var sundayNoon = sunday.ToDateTime(new TimeOnly(12, 0));
                var sundayEnd = sunday.ToDateTime(new TimeOnly(23, 59));

                DateTime now = DateTime.Now;
                DateTime twoWeeksFromNow = now.AddDays(14);

                var refereeMatches = listOfMatches.Where(m =>
                    m.RefereeId == referee.RefereeId ||
                    m.Ar1Id == referee.RefereeId ||
                    m.Ar2Id == referee.RefereeId
                ).ToList();
                // time options based on matches (referee cannot be delegated when he has time at the time of delegation) production
                /*foreach (var match in listOfMatches.Where(m =>
                {
                    DateTime matchDateTime = m.Match.MatchDate.ToDateTime(m.Match.MatchTime);
                    return matchDateTime >= now && matchDateTime <= twoWeeksFromNow;
                }))*/
                foreach (var match in refereeMatches)
                {
                    // Calculate the time window the referee is unavailable due to a match:
                    // - Subtract pre-match waiting time
                    // - Add double match duration (halves), 15 minutes break, and post-match buffer
                    DateTime officialMatchStart = match.MatchDate.ToDateTime(match.MatchTime);
                    DateTime matchStart = officialMatchStart.AddMinutes(-waitingTimeBeforeMatch);
                    DateTime matchEnd = officialMatchStart.AddMinutes((match.Competition.MatchLength * 2) + 15 + reserveAfterMatches);
                    var timeFlags = GetMatchTimeFlags(matchStart, firstGameDay, saturdayStart, saturdayNoon, sundayStart, sundayNoon, sundayEnd);

                    RefereeWithTimeOptions.TimeRange timeRange;
                    // Differentiate between main referee ("zapasref") and assistants ("zapasar")
                    // to allow finer filtering later.
                    if (match.RefereeId == refereeWithTimeOptions.Referee.RefereeId)
                    {
                        timeRange = new RefereeWithTimeOptions.TimeRange(matchStart, matchEnd, "zapasref", matchId: match.MatchId);
                    }
                    else
                    {
                        timeRange = new RefereeWithTimeOptions.TimeRange(matchStart, matchEnd, "zapasar", matchId: match.MatchId);
                    }

                    refereeWithTimeOptions.SortedRanges.Add(timeRange);
                    SetTimeFlags(timeFlags, refereeWithTimeOptions);

                }
                // time options based on excuses (referee cannot be delegated when they have an excuse during that time)
                // Excuses are treated as unavailable time slots for the referee.
                // These could overlap with match or vehicle slots and must be considered.
                if (referee.Excuses != null && referee.Excuses.Any())
                {
                    foreach (var excuse in referee.Excuses)
                    {
                        DateTime excuseStart = excuse.DateFrom.ToDateTime(excuse.TimeFrom);
                        DateTime excuseEnd = excuse.DateTo.ToDateTime(excuse.TimeTo);

                        var timeFlags = GetExcuseTimeFlags(excuseStart, excuseEnd, firstGameDay, saturdayStart, saturdayNoon, sundayStart, sundayNoon, sundayEnd);

                        //check if it is the ongoing excuse or start is in two weeks production
                        /*if ((excuseStart >= now && excuseStart <= twoWeeksFromNow) || (excuseStart <= now && excuseEnd >= now))
                        {*/

                        RefereeWithTimeOptions.TimeRange timeRange = new RefereeWithTimeOptions.TimeRange(excuseStart, excuseEnd, "omluva", excuseId: excuse.ExcuseId);

                        if (!string.IsNullOrWhiteSpace(excuse.Note))
                        {
                            refereeWithTimeOptions.hasSpecialNote = true;
                        }
                        refereeWithTimeOptions.SortedRanges.Add(timeRange);
                        SetTimeFlags(timeFlags, refereeWithTimeOptions);

                        //}
                    }
                }

                // time options based on vehicleSlots (referee possibilities with vehicle, they are not affecting time availability of referee when we delegate him)

                if (referee.VehicleSlots != null && referee.VehicleSlots.Any())
                {
                    foreach (var vehicleSlot in referee.VehicleSlots)
                    {
                        DateTime vehicleSlotStart = vehicleSlot.DateFrom.ToDateTime(vehicleSlot.TimeFrom);
                        DateTime vehicleSlotEnd = vehicleSlot.DateTo.ToDateTime(vehicleSlot.TimeTo);
                        //check if it is the ongoing vehicleSlot or start is in two weeks production
                        /*if ((vehicleSlotStart >= now && vehicleSlotStart <= twoWeeksFromNow) || (vehicleSlotStart <= now && vehicleSlotEnd >= now))
                        {*/
                        RefereeWithTimeOptions.TimeRange timeRange = new RefereeWithTimeOptions.TimeRange(vehicleSlotStart, vehicleSlotEnd, "vozidlo", slotId: vehicleSlot.SlotId);

                        refereeWithTimeOptions.SortedRanges.Add(timeRange);
                        //}

                    }
                }
                //transfers between matches (they are not affecting time availability of referee when we delegate him)
                foreach (var transfer in listOfTransfers)
                {
                    RefereeWithTimeOptions.TimeRange timeRange = new RefereeWithTimeOptions.TimeRange(transfer.ExpectedDeparture, transfer.ExpectedArrival, "transfer", transferId: transfer.TransferId);
                    refereeWithTimeOptions.SortedRanges.Add(timeRange);
                }



                return ServiceResult<RefereeWithTimeOptions>.Success(refereeWithTimeOptions);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddRefereeTimeOptionsAsync] Error getting time options of referees");
                return ServiceResult<RefereeWithTimeOptions>.Failure("Nepodařilo se získat časové možnosti rozhodčího!");
            }
        }
	public async Task<ServiceResult<TransportBetweenMatchesViewModel>> CalculateTransfersWhenAssigningAsync(Models.Match matchToCheck, RefereeWithTimeOptions refereeToAssign,bool force)
        {
            try
            {
                //find out if there is longtitute and latitude of play field
                Tuple<bool, double, Transfer> isManageableWTransferPreMatch = null!;
                Tuple<bool, double, Transfer> isManageableWTransferPostMatch = null!;
                Tuple<DateTime, string?> startOfNextMatch = null!;
                Tuple<DateTime, string?> endOfPreviousMatch = null!;
                Transfer transferPre = null!;
                Transfer transferPost = null!;
                bool succesfullyCalculatedRoutePre = true;
                bool succesfullyCalculatedRoutePost = true;



                //find if the location is not set
                float longtitude = matchToCheck.Field.Longitude;
                float latitude = matchToCheck.Field.Latitude;
                const float epsilon = 0.001f;

                bool isLatLonZero = Math.Abs(longtitude) < epsilon || Math.Abs(latitude) < epsilon;

                if (!isLatLonZero)
                {
                    startOfNextMatch = GetFirstNextMatchDateTime(refereeToAssign, matchToCheck).GetDataOrThrow();
                    endOfPreviousMatch = GetFirstPreviousMatchDateTime(refereeToAssign, matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime)).GetDataOrThrow();

                    bool hasCarOverall = refereeToAssign.Referee.CarAvailability;
                    //referee is using car or public transport at the moment?
                    bool? hasCarDuring = CheckCarAvailabilityOfReferee(refereeToAssign, matchToCheck).GetDataOrThrow();

                    bool actuallCarAvailability = hasCarDuring.HasValue ? hasCarDuring.Value : hasCarOverall;
                    //calculate the possible route with km and time if the match is less than 1 hour and half from the beggining of actuall
                    if (startOfNextMatch != null)
                    {
                        var nextMatch = (await _adminRepo.GetMatchByIdAsync(startOfNextMatch.Item2)).GetDataOrThrow();

                        bool isEndingLatLonZero = Math.Abs(nextMatch.Field.Longitude) < epsilon || Math.Abs(nextMatch.Field.Latitude) < epsilon;
                        try
                        {
                            if (!isEndingLatLonZero && actuallCarAvailability)
                            {
                                var result = (await _routeCarPlanner.CalculateRoute(latitude, longtitude, nextMatch.Field.Latitude, nextMatch.Field.Longitude)).GetDataOrThrow();
                                isManageableWTransferPostMatch = CheckTimeAvailabilityWithTransferOfReferee(startOfNextMatch.Item1, nextMatch.MatchId, matchToCheck, result.Item2, refereeToAssign.Referee.RefereeId, false, true).GetDataOrThrow();
                                transferPost = isManageableWTransferPostMatch.Item3;
                            }
                            else if (!isEndingLatLonZero && !actuallCarAvailability)
                            {
                                var result = (await _routeBusPlanner.CalculateRoute(latitude, longtitude, nextMatch.Field.Latitude, nextMatch.Field.Longitude, matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime).AddMinutes(135))).GetDataOrThrow();
                                isManageableWTransferPostMatch = CheckTimeAvailabilityWithTransferOfReferee(startOfNextMatch.Item1, nextMatch.MatchId, matchToCheck, result.Item2, refereeToAssign.Referee.RefereeId, false, true).GetDataOrThrow();
                                transferPost = isManageableWTransferPostMatch.Item3;
                            }
                        }
                        catch (Exception ex)
                        {
                            succesfullyCalculatedRoutePost = false;
                            _logger.LogError(ex, "[Calculating Route] Route or transfer check failed, continuing.");
                        }
                    }
                    if (endOfPreviousMatch != null)
                    {
                        try
                        {
                            var previousMatch = (await _adminRepo.GetMatchByIdAsync(endOfPreviousMatch.Item2)).GetDataOrThrow();

                            bool isEndingLatLonZero = Math.Abs(previousMatch.Field.Longitude) < epsilon || Math.Abs(previousMatch.Field.Latitude) < epsilon;
                            if (!isEndingLatLonZero && actuallCarAvailability)
                            {
                                var result = (await _routeCarPlanner.CalculateRoute(previousMatch.Field.Latitude, previousMatch.Field.Longitude, latitude, longtitude)).GetDataOrThrow();
                                isManageableWTransferPreMatch = CheckTimeAvailabilityWithTransferOfReferee(endOfPreviousMatch.Item1, previousMatch.MatchId, matchToCheck, result.Item2, refereeToAssign.Referee.RefereeId, true, true).GetDataOrThrow();
                                transferPre = isManageableWTransferPreMatch.Item3;
                            }
                            else if (!isEndingLatLonZero && !actuallCarAvailability)
                            {
                                var result = (await _routeBusPlanner.CalculateRoute(previousMatch.Field.Latitude, previousMatch.Field.Longitude, latitude, longtitude, endOfPreviousMatch.Item1)).GetDataOrThrow();
                                isManageableWTransferPreMatch = CheckTimeAvailabilityWithTransferOfReferee(endOfPreviousMatch.Item1, previousMatch.MatchId, matchToCheck, result.Item2, refereeToAssign.Referee.RefereeId, true, true).GetDataOrThrow();
                                transferPre = isManageableWTransferPreMatch.Item3;
                            }
                        }
                        catch (Exception ex)
                        {
                            succesfullyCalculatedRoutePre = false;
                            _logger.LogError(ex, "[Calculating Route] Route or transfer check failed, continuing.");
                        }
                    }
                }
                if (isManageableWTransferPostMatch != null && succesfullyCalculatedRoutePost)
                {
                    if (!force && !isManageableWTransferPostMatch.Item1)
                    {

                        return ServiceResult<TransportBetweenMatchesViewModel>.Success(new TransportBetweenMatchesViewModel(false, "Daný rozhodčí nestíhá přijít na zápas z předzápasu o " + isManageableWTransferPostMatch.Item2 + " minut (zkontrolujte v okně rozhodčího)!",null,null));
                    }
                }
                if (isManageableWTransferPreMatch != null && succesfullyCalculatedRoutePre)
                {
                    if (!force && !isManageableWTransferPreMatch.Item1)
                    {
                        return ServiceResult<TransportBetweenMatchesViewModel>.Success(new TransportBetweenMatchesViewModel(false, "Daný rozhodčí nestíhá přijít na následující zápas z tohoto zápasu o" + isManageableWTransferPreMatch.Item2 + " minut (zkontrolujte v okně rozhodčího)!",null,null));
                    }
                }
                return ServiceResult<TransportBetweenMatchesViewModel>.Success(new TransportBetweenMatchesViewModel(true, "",transferPre,transferPost));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddRefereeToTheMatch] Error calculating transfer when delegating referee");
                return ServiceResult<TransportBetweenMatchesViewModel>.Failure("Neočekávaná chyba při vypočítávání trasy mezi zápasy rozhodčímu!");

            }
        }
        public async Task<ServiceResult<List<RefereeWithTimeOptions>>> AddRefereesTimeOptionsAsync(List<Referee> listWithReferees, List<MatchViewModel> listOfMatches, List<Transfer> listOfTransfers, DateOnly firstGameDay)
        {
            try
            {


                List<RefereeWithTimeOptions> refereesWithSortedLists = listWithReferees.Select(r => new RefereeWithTimeOptions(r)).ToList();

                // Create a dictionary for lookup of referees by ID
                Dictionary<int, RefereeWithTimeOptions> refereeDict = new Dictionary<int, RefereeWithTimeOptions>();
                foreach (var refereeWithSortList in refereesWithSortedLists)
                {
                    refereeDict[refereeWithSortList.Referee.RefereeId] = refereeWithSortList;
                }
                // Define key time points for the weekend schedule:
                // - Saturday early morning (00:01)
                // - Saturday noon (12:00)
                // - Sunday early morning (00:01)
                // - Sunday noon (12:00)
                // - Sunday end (23:59)
                // These time points are used to categorize referee availability into time slots
                var saturdayStart = firstGameDay.ToDateTime(new TimeOnly(0, 1));
                var saturdayNoon = firstGameDay.ToDateTime(new TimeOnly(12, 0));

                var sunday = firstGameDay.AddDays(1);
                var sundayStart = sunday.ToDateTime(new TimeOnly(0, 1));
                var sundayNoon = sunday.ToDateTime(new TimeOnly(12, 0));
                var sundayEnd = sunday.ToDateTime(new TimeOnly(23, 59));
                //production i work with old data
                DateTime now = DateTime.Now;
                DateTime twoWeeksFromNow = now.AddDays(14);
                // time options based on matches (referee cannot be delegated when he has time at the time of delegation)
                /*foreach (var match in listOfMatches.Where(m =>
                {
                    DateTime matchDateTime = m.Match.MatchDate.ToDateTime(m.Match.MatchTime);
                    return matchDateTime >= now && matchDateTime <= twoWeeksFromNow;
                }))*/

                foreach (var match in listOfMatches)
                {
                    // Calculate the time window the referee is unavailable due to a match:
                    // - Subtract pre-match waiting time
                    // - Add double match duration (halves), 15 minutes break, and post-match buffer
                    DateTime officialMatchStart = match.Match.MatchDate.ToDateTime(match.Match.MatchTime);
                    DateTime matchStart = officialMatchStart.AddMinutes(-waitingTimeBeforeMatch);
                    DateTime matchEnd = officialMatchStart.AddMinutes((match.Match.Competition.MatchLength * 2) + 15 + reserveAfterMatches);
                    var timeFlags = GetMatchTimeFlags(matchStart, firstGameDay, saturdayStart, saturdayNoon, sundayStart, sundayNoon, sundayEnd);

                    RefereeWithTimeOptions.TimeRange timeRange = new RefereeWithTimeOptions.TimeRange(matchStart, matchEnd, "zapas", matchId: match.Match.MatchId);
                    // Differentiate between main referee ("zapasref") and assistants ("zapasar")
                    // to allow finer filtering later.
                    if (match.Match.RefereeId.HasValue && refereeDict.TryGetValue(match.Match.RefereeId.Value, out RefereeWithTimeOptions mainRefTimeOptions))
                    {
                        timeRange.RangeType = "zapasref";
                        mainRefTimeOptions.SortedRanges.Add(timeRange);
                        SetTimeFlags(timeFlags, mainRefTimeOptions);
                    }

                    if (match.Match.Ar1Id.HasValue && refereeDict.TryGetValue(match.Match.Ar1Id.Value, out RefereeWithTimeOptions ar1TimeOptions))
                    {
                        timeRange.RangeType = "zapasar";
                        ar1TimeOptions.SortedRanges.Add(timeRange);
                        SetTimeFlags(timeFlags, ar1TimeOptions);
                    }

                    if (match.Match.Ar2Id.HasValue && refereeDict.TryGetValue(match.Match.Ar2Id.Value, out RefereeWithTimeOptions ar2TimeOptions))
                    {
                        timeRange.RangeType = "zapasar";
                        ar2TimeOptions.SortedRanges.Add(timeRange);
                        SetTimeFlags(timeFlags, ar2TimeOptions);
                    }
                }
                // time options based on excuses (referee cannot be delegated when they have an excuse during that time)
                // Excuses are treated as unavailable time slots for the referee.
                // These could overlap with match or vehicle slots and must be considered.
                foreach (var referee in listWithReferees)
                {
                    if (referee.Excuses != null && referee.Excuses.Any())
                    {
                        foreach (var excuse in referee.Excuses)
                        {
                            DateTime excuseStart = excuse.DateFrom.ToDateTime(excuse.TimeFrom);
                            DateTime excuseEnd = excuse.DateTo.ToDateTime(excuse.TimeTo);

                            var timeFlags = GetExcuseTimeFlags(excuseStart, excuseEnd, firstGameDay, saturdayStart, saturdayNoon, sundayStart, sundayNoon, sundayEnd);

                            //check if it is the ongoing excuse or start is in two weeks
                            if ((excuseStart >= now && excuseStart <= twoWeeksFromNow) || (excuseStart <= now && excuseEnd >= now))
                            {

                                RefereeWithTimeOptions.TimeRange timeRange = new RefereeWithTimeOptions.TimeRange(excuseStart, excuseEnd, "omluva", excuseId: excuse.ExcuseId);


                                if (refereeDict.TryGetValue(referee.RefereeId, out RefereeWithTimeOptions refTimeOptions))
                                {
                                    refTimeOptions.SortedRanges.Add(timeRange);
                                    if (!string.IsNullOrWhiteSpace(excuse.Note))
                                    {
                                        refTimeOptions.hasSpecialNote = true;
                                    }
                                    SetTimeFlags(timeFlags, refTimeOptions);
                                }
                            }
                        }
                    }
                }

                // time options based on vehicleSlots (referee possibilities with vehicle, they are not affecting time availability of referee when we delegate him)
                foreach (var referee in listWithReferees)
                {
                    if (referee.VehicleSlots != null && referee.VehicleSlots.Any())
                    {
                        foreach (var vehicleSlot in referee.VehicleSlots)
                        {
                            DateTime vehicleSlotStart = vehicleSlot.DateFrom.ToDateTime(vehicleSlot.TimeFrom);
                            DateTime vehicleSlotEnd = vehicleSlot.DateTo.ToDateTime(vehicleSlot.TimeTo);
                            //check if it is the ongoing vehicleSlot or start is in two weeks production
                            /*if ((vehicleSlotStart >= now && vehicleSlotStart <= twoWeeksFromNow) || (vehicleSlotStart <= now && vehicleSlotEnd >= now))
                            {*/
                            RefereeWithTimeOptions.TimeRange timeRange = new RefereeWithTimeOptions.TimeRange(vehicleSlotStart, vehicleSlotEnd, "vozidlo", slotId: vehicleSlot.SlotId);


                            if (refereeDict.TryGetValue(referee.RefereeId, out RefereeWithTimeOptions refTimeOptions))
                            {
                                refTimeOptions.SortedRanges.Add(timeRange);
                            }
                            //}

                        }
                    }
                }
                //transfers between matches (they are not affecting time availability of referee when we delegate him)
                foreach (var transfer in listOfTransfers)
                {
                    RefereeWithTimeOptions.TimeRange timeRange = new RefereeWithTimeOptions.TimeRange(transfer.ExpectedDeparture, transfer.ExpectedArrival, "transfer", transferId: transfer.TransferId);
                    if (refereeDict.TryGetValue(transfer.RefereeId, out RefereeWithTimeOptions refTimeOptions))
                    {
                        refTimeOptions.SortedRanges.Add(timeRange);
                    }
                }
                return ServiceResult<List<RefereeWithTimeOptions>>.Success(refereesWithSortedLists);

            } catch (Exception ex)
            {
                _logger.LogError(ex, "[AddTimeOptionsAsync] Error getting time options of referees");
                return ServiceResult<List<RefereeWithTimeOptions>>.Failure("Nepodařilo se získat časové možnosti rozhodčích!");
            }
        }

        public ServiceResult<Tuple<DateTime, string?>> GetFirstNextMatchDateTime(RefereeWithTimeOptions referee, Models.Match matchToCheck)
        {
            try
            {

                var matchesOfReferee = referee.SortedRanges.Where(s => s.RangeType == "zapasref" || s.RangeType == "zapasar");
                if (referee.SortedRanges == null || !matchesOfReferee.Any())
                {
                    return ServiceResult<Tuple<DateTime, string?>>.Success(null);
                }

                DateTime matchStart = matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime);
                TimeRange nextRange = matchesOfReferee
                    .Where(range => range.Start > matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime))
                    .OrderBy(range => range.Start)
                    .FirstOrDefault();
                DateTime matchEnd = matchStart.AddMinutes((matchToCheck.Competition.MatchLength * 2) + 15 + reserveAfterMatches);



                if (nextRange == null)
                {
                    return ServiceResult<Tuple<DateTime, string?>>.Success(null);
                }
                //filter the matches whose are later than 2 hours after match end
                if (nextRange.Start.AddMinutes(-travellingMaximumPeriod) > matchEnd)
                {
                    return ServiceResult<Tuple<DateTime, string?>>.Success(null);
                }

                return ServiceResult<Tuple<DateTime, string?>>.Success(new Tuple<DateTime, string?>(nextRange.Start, nextRange.MatchId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetFirstNextMatchDateTime] Error in proccess of finding out the next match id");
                return ServiceResult<Tuple<DateTime, string?>>.Failure("Chyba při získavání id nasledujícího zápasů");
            }
        }
        public ServiceResult<Tuple<DateTime, string?>> GetFirstPreviousMatchDateTime(RefereeWithTimeOptions referee, DateTime matchStart)
        {
            try
            {
                var matchesOfReferee = referee.SortedRanges.Where(s => s.RangeType == "zapasref" || s.RangeType == "zapasar");
                if (referee.SortedRanges == null || !matchesOfReferee.Any())
                {
                    return ServiceResult<Tuple<DateTime, string?>>.Success(null);
                }

                TimeRange nextRange = matchesOfReferee
                    .Where(range => range.Start < matchStart)
                    .OrderByDescending(range => range.Start)
                    .FirstOrDefault();

                if (nextRange == null)
                {
                    return ServiceResult<Tuple<DateTime, string?>>.Success(null);
                }
                //filter the matches whose are later than 2 hours and 15 minutes before match start
                if (nextRange.End.AddMinutes(travellingMaximumPeriod) < matchStart.AddMinutes(-waitingTimeBeforeMatch))
                {
                    return ServiceResult<Tuple<DateTime, string?>>.Success(null);
                }
                return ServiceResult<Tuple<DateTime, string?>>.Success(new Tuple<DateTime, string?>(nextRange.End, nextRange.MatchId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetFirstPreviousMatchDateTime] Error in proccess of finding out the previous match id");
                return ServiceResult<Tuple<DateTime, string?>>.Failure("Chyba při získavání id predošlýho zápasů");
            }
        }
        public ServiceResult<Tuple<float, float>> GetLocationBeforeMatch(List<Models.Match> listOfMatches, DateTime startDateTime)
        {
            try
            {
                // Find the latest match that occurred before the given startDateTime
                var previousMatch = listOfMatches
                .Where(m => m.MatchDate.ToDateTime(m.MatchTime) < startDateTime)
                .OrderByDescending(m => m.MatchDate.ToDateTime(m.MatchTime))
                .FirstOrDefault();

                if (previousMatch == null)
                {
                    return ServiceResult<Tuple<float, float>>.Success(null);
                }

                var timeDifference = startDateTime - previousMatch.MatchDate.ToDateTime(previousMatch.MatchTime);

                if (timeDifference.TotalHours <= locationToBeRelevantTimeGap)
                {
                    var location = Tuple.Create(previousMatch.Field.Latitude, previousMatch.Field.Longitude);
                    return ServiceResult<Tuple<float, float>>.Success(location);
                }
                else
                {
                    return ServiceResult<Tuple<float, float>>.Success(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetLocationBeforeMatch] Error in proccess of finding out the previous location");
                return ServiceResult<Tuple<float, float>>.Failure("Chyba při získavání lokace predošlýho zápasů");
            }

        }
        public ServiceResult<Tuple<float, float>> GetLocationAfterMatch(List<Models.Match> listOfMatches, DateTime startDateTime)
        {
            try
            {
                var nextMatch = listOfMatches
               .Where(m => m.MatchDate.ToDateTime(m.MatchTime) > startDateTime)
               .OrderBy(m => m.MatchDate.ToDateTime(m.MatchTime))
               .FirstOrDefault();

                if (nextMatch == null)
                {
                    return ServiceResult<Tuple<float, float>>.Success(null);
                }

                var timeDifference = nextMatch.MatchDate.ToDateTime(nextMatch.MatchTime) - startDateTime;

                if (timeDifference.TotalHours <= locationToBeRelevantTimeGap)
                {
                    var location = Tuple.Create(nextMatch.Field.Latitude, nextMatch.Field.Longitude);
                    return ServiceResult<Tuple<float, float>>.Success(location);
                }
                else
                {
                    return ServiceResult<Tuple<float, float>>.Success(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetLocationAfterMatch] Error in proccess of finding out the next location");
                return ServiceResult<Tuple<float, float>>.Failure("Chyba při získavání lokace nasledujícího zápasu");
            }
        }
        public ServiceResult<Dictionary<int, Tuple<int, string>>> CalculatePointsForReferees(List<int> refereeIds, Dictionary<int, bool> isFreeDuringTheMatch, Dictionary<int,int> ratingOfReferees, List<RefereesTeamsMatchesResponseDto> homeMatches, List<RefereesTeamsMatchesResponseDto> awayMatches, List<RefereesMatchesResponseDto> totalMatches, Dictionary<int, int> distanceDictionary)
        {
            // Constants for penalty calculations
            const int STARTING_POINTS = 100;
            const int VETO_PENALTY = -40;

            const int TEAM_PENALTY_FIRST_TWO_AT_HOME = -3;
            const int TEAM_PENALTY_THIRD_AT_HOME = -5;
            const int TEAM_PENALTY_ADDITIONAL_AT_HOME = -10;

            const int TEAM_PENALTY_FIRST_TWO_AWAY = -2;
            const int TEAM_PENALTY_THIRD_AWAY = -3;
            const int TEAM_PENALTY_ADDITIONAL_AWAY = -5;

            const int DISTANCE_PENALTY_PER_5KM = -3;

            const int WEEKEND_PENALTY_PER_MATCH_AR = -2;
            const int WEEKEND_PENALTY_PER_MATCH_R = -3;
	    
            try
            {
		// Convert match data into dictionaries for fast lookup by refereeId
                var homeMatchesByReferee = homeMatches.ToDictionary(x => x.RefereeId);
                var awayMatchesByReferee = awayMatches.ToDictionary(x => x.RefereeId);
                var totalMatchesByReferee = totalMatches.ToDictionary(x => x.RefereeId);

                var result = new Dictionary<int, Tuple<int, string>>();

                foreach (var refereeId in refereeIds)
                {
                        var reasonBuilder = new System.Text.StringBuilder();

                        var isFree = isFreeDuringTheMatch[refereeId];
                        if (!isFree)
                        {
                            reasonBuilder.AppendLine($"Rozhodčí nění dostupný během zápasu");
                            result.Add(refereeId, new Tuple<int, string>(1, reasonBuilder.ToString()));
                            continue;
                        }
                        int totalPoints = STARTING_POINTS;
                        var homeMatchData = homeMatchesByReferee.GetValueOrDefault(refereeId, new RefereesTeamsMatchesResponseDto { RefereeId = refereeId, HomeCount = 0, AwayCount = 0 });
                        var awayMatchData = awayMatchesByReferee.GetValueOrDefault(refereeId, new RefereesTeamsMatchesResponseDto { RefereeId = refereeId, HomeCount = 0, AwayCount = 0 });
                        var matchTotals = totalMatchesByReferee.GetValueOrDefault(refereeId, new RefereesMatchesResponseDto { RefereeId = refereeId, asAssistantReferee = 0, asReferee = 0 });

                        var distance = distanceDictionary.GetValueOrDefault(refereeId,0);
			            var rating = ratingOfReferees[refereeId];


                        reasonBuilder.AppendLine($"Domácí tým");
                        totalPoints += CalculateTeamMatchesPenalty(homeMatchData, reasonBuilder);
                        reasonBuilder.AppendLine($"Hostující tým");
                        totalPoints += CalculateTeamMatchesPenalty(awayMatchData, reasonBuilder);
                        totalPoints += (distance / 5) * DISTANCE_PENALTY_PER_5KM;
                        reasonBuilder.AppendLine($"Body za lokaci :{distance} km,    =>body: {(distance / 5) * DISTANCE_PENALTY_PER_5KM}");
                        int minusPointsTotals = matchTotals.asReferee * WEEKEND_PENALTY_PER_MATCH_R + matchTotals.asAssistantReferee * WEEKEND_PENALTY_PER_MATCH_AR;
                        totalPoints += minusPointsTotals;
                        reasonBuilder.AppendLine($"Jako rozhodčí :{matchTotals.asReferee} zápasů");
                        reasonBuilder.AppendLine($"Jako asistent :{matchTotals.asAssistantReferee} zápasů");
			                totalPoints -= (10 - rating);
                        reasonBuilder.AppendLine($" =>body: {minusPointsTotals}");
			            reasonBuilder.AppendLine($"Rating : -{10 - rating}");

                        // Ensure score does not go below zero
                        int finalScore = totalPoints < 0 ? 0 : totalPoints;
                        reasonBuilder.AppendLine($"Konečné skóre: {finalScore}");                    

                        result.Add(refereeId, new Tuple<int, string>(finalScore, reasonBuilder.ToString()));
                    }

                foreach (var kvp in result)
                {
                    _logger.LogInformation($"{kvp.Key} w> {kvp.Value}");
                }
                return ServiceResult<Dictionary<int, Tuple<int, string>>>.Success(result);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[CalculatePointsForReferees] Error finding out the points for referees ");
                return ServiceResult< Dictionary<int, Tuple<int, string>>>.Failure("Nepodařilo se získat body rozhodčích!");
            }
            // Helper method for team matches penalty
            int CalculateTeamMatchesPenalty(
                RefereesTeamsMatchesResponseDto matches,
                System.Text.StringBuilder reasonBuilder)
            {               
                int teamsPenaltyHome = 0;

                if (matches.HomeCount <= 2)
                {
                    teamsPenaltyHome = matches.HomeCount * TEAM_PENALTY_FIRST_TWO_AT_HOME;
                }
                else if (matches.HomeCount == 3)
                {
                    teamsPenaltyHome = 2 * TEAM_PENALTY_FIRST_TWO_AT_HOME + TEAM_PENALTY_THIRD_AT_HOME;
                }
                else if (matches.HomeCount >= 4 && matches.HomeCount <= 5)
                {
                    teamsPenaltyHome = 2 * TEAM_PENALTY_FIRST_TWO_AT_HOME + TEAM_PENALTY_THIRD_AT_HOME +
                                  ((matches.HomeCount - 3) * TEAM_PENALTY_ADDITIONAL_AT_HOME);
                }else
                {
                    teamsPenaltyHome = VETO_PENALTY;
                }
                int teamsPenaltyAway = 0;

                if (matches.AwayCount <= 2)
                {
                    teamsPenaltyAway = matches.AwayCount * TEAM_PENALTY_FIRST_TWO_AWAY;
                }
                else if (matches.AwayCount == 3)
                {
                    teamsPenaltyAway = 2 * TEAM_PENALTY_FIRST_TWO_AWAY + TEAM_PENALTY_THIRD_AWAY;
                }
                else if (matches.AwayCount >= 4 && matches.AwayCount <= 5)
                {
                    teamsPenaltyAway = 2 * TEAM_PENALTY_FIRST_TWO_AWAY + TEAM_PENALTY_THIRD_AWAY +
                                  ((matches.AwayCount - 3) * TEAM_PENALTY_ADDITIONAL_AWAY);
                }
                else
                {
                    teamsPenaltyAway = VETO_PENALTY;
                }

                int finalPenalty = teamsPenaltyHome + teamsPenaltyAway;
                reasonBuilder.AppendLine($"zápasy doma: {matches.HomeCount} ,vonku:{matches.AwayCount} ,body => {finalPenalty}");
                return finalPenalty;
            }                 
        }

        public ServiceResult<bool> CheckTimeAvailabilityOfReferee(RefereeWithTimeOptions referee, Models.Match matchToCheck)
        {
            try
            {
                DateTime matchStart = matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime).AddMinutes(-waitingTimeBeforeMatch); // 1 hour before match
                DateTime matchEnd = matchStart.AddMinutes(matchToCheck.Competition.MatchLength * 2 + 15 + reserveAfterMatches); // Match duration + halftime + reserve 

                foreach (var existingRange in referee.SortedRanges.Where(s => s.RangeType != "vozidlo" && s.RangeType !="transfer"))
                {
                    if (matchStart <= existingRange.End && matchEnd >= existingRange.Start)
                    {
                        return ServiceResult<bool>.Success(false);
                    }
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CheckTimeAvailabilityOfReferee] Error finding out the availability of referee in specific match time");
                return ServiceResult<bool>.Failure("Nepodařilo se získat zda je rozhodčí dostupný v danej zápas!");
            }
        }
        public ServiceResult<bool?> CheckCarAvailabilityOfReferee(RefereeWithTimeOptions referee,Models.Match matchToCheck)
        {
            try
            {
                DateTime matchStart = matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime).AddMinutes(-waitingTimeBeforeMatch); // approx hour before match
                DateTime matchEnd = matchStart.AddMinutes(matchToCheck.Competition.MatchLength * 2 + 15 + reserveAfterMatches); // Match duration + halftime + reserve 

                foreach (var existingRange in referee.SortedRanges.Where(s => s.RangeType == "vozidlo"))
                {
                    if (matchStart <= existingRange.End && matchEnd >= existingRange.Start)
                    {
                        var vehicleSlot = referee.Referee.VehicleSlots.FirstOrDefault(v => v.SlotId == existingRange.SlotId);
                        if(vehicleSlot!=null)
                            return ServiceResult<bool?>.Success(vehicleSlot.HasCarInTheSlot);
                    }
                }

                return ServiceResult<bool?>.Success(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CheckCarAvailabilityOfReferee] Error finding out the availability of referee cars throughout specific match time");
                return ServiceResult<bool?>.Failure("Nepodařilo se získat informaci zda je rozhodčího dostupný s autem v průbehu daného zápasu!");
            }
        }
        public ServiceResult<Tuple<bool,double,Transfer>> CheckTimeAvailabilityWithTransferOfReferee(DateTime startOrEndOfConnectedMatch, string connectedMatchId, Models.Match matchToCheck, int transferLength,int refereeId,bool isPreMatch,bool hasCar)
        {
            try
            {
                DateTime matchStart = matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime).AddMinutes(-waitingTimeBeforeMatch); // 1 hour before match
                DateTime matchEnd = matchToCheck.MatchDate.ToDateTime(matchToCheck.MatchTime).AddMinutes(matchToCheck.Competition.MatchLength * 2 + 15 + reserveAfterMatches); // Match duration + halftime + reserve 

                DateTime endOfPreviousWithTransfer = startOrEndOfConnectedMatch.AddMinutes(transferLength);
                DateTime startOfNextWithTransfer = startOrEndOfConnectedMatch.AddMinutes(-transferLength);

                if (isPreMatch)
                {
                    TimeSpan difference = endOfPreviousWithTransfer - matchStart;
                    double minutesDifference = difference.TotalMinutes;

                    Transfer transfer = new Transfer
                    {
                        RefereeId = refereeId,
                        PreviousMatchId = connectedMatchId,
                        FutureMatchId = matchToCheck.MatchId,
                        ExpectedDeparture = matchStart.AddMinutes(-transferLength),
                        ExpectedArrival = matchStart,                      
                        FromHome = false,
                        Car = hasCar
                    };

                    if (matchStart < endOfPreviousWithTransfer)
                    {                      
                        return ServiceResult<Tuple<bool, double,Transfer>>.Success(new Tuple<bool, double,Transfer>(false,minutesDifference,transfer));
                    }
                    else
                    {
                        return ServiceResult<Tuple<bool, double,Transfer>>.Success(new Tuple<bool, double,Transfer>(true, minutesDifference,transfer));
                    }
                }
                else
                {
                    TimeSpan difference = matchEnd - startOfNextWithTransfer;
                    double minutesDifference = difference.TotalMinutes;

                    Transfer transfer = new Transfer
                    {
                        RefereeId = refereeId,
                        PreviousMatchId = matchToCheck.MatchId,
                        FutureMatchId = connectedMatchId,
                        ExpectedDeparture = matchEnd,
                        ExpectedArrival = matchEnd.AddMinutes(transferLength),
                        FromHome = false,
                        Car = hasCar
                    };

                    if (matchEnd > startOfNextWithTransfer)
                    {
                        return ServiceResult<Tuple<bool, double,Transfer>>.Success(new Tuple<bool, double,Transfer>(false, minutesDifference,transfer));
                    }
                    else
                    {
                        return ServiceResult<Tuple<bool, double,Transfer>>.Success(new Tuple<bool, double,Transfer>(true, minutesDifference,transfer));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CheckTimeAvailabilityWithTransferOfReferee] Error finding out the availability of referee in specific match time with transfer");
                return ServiceResult<Tuple<bool,double,Transfer>>.Failure("Nepodařilo se získat zda je rozhodčí dostupný v danej zápas s transferom!");
            }

        }
        public ServiceResult<Dictionary<int, string>> GetRefereeDictionary(List<Referee> listOfReferees)
         {
            try
            {               
                var dict = listOfReferees.ToDictionary(
                        referee => referee.RefereeId,
                        referee => $"{referee.Name.Substring(0, 1)}. {referee.Surname}"
                    );
                return ServiceResult<Dictionary<int, string>>.Success(dict);
            }catch(Exception ex)
            {
                _logger.LogError(ex, "[GetRefereeDictionary] Error getting names of referees");
                return ServiceResult<Dictionary<int, string>>.Failure("Nepodařilo se získat jména a id rozhodčích!");
            }
        }

        public record TimeFlags(
            bool SaturdayMorning,
            bool SaturdayAfternoon,
            bool SundayMorning,
            bool SundayAfternoon
        );
        private TimeFlags GetMatchTimeFlags(DateTime matchStart, DateOnly firstGameDay, DateTime saturdayStart, DateTime saturdayNoon, DateTime sundayStart, DateTime sundayNoon, DateTime sundayEnd)
        { 
            return new TimeFlags(
                SaturdayMorning: matchStart >= saturdayStart && matchStart < saturdayNoon,
                SaturdayAfternoon: matchStart >= saturdayNoon && matchStart < sundayStart,
                SundayMorning: matchStart >= sundayStart && matchStart < sundayNoon,
                SundayAfternoon: matchStart >= sundayNoon && matchStart <= sundayEnd
            );
        }
        private TimeFlags GetExcuseTimeFlags(DateTime excuseStart, DateTime excuseEnd, DateOnly firstGameDay, DateTime saturdayStart, DateTime saturdayNoon, DateTime sundayStart, DateTime sundayNoon, DateTime sundayEnd)
        {
            bool Overlaps(DateTime exStart, DateTime exEnd, DateTime rangeStart, DateTime rangeEnd)
            {
                return exStart < rangeEnd && rangeStart < exEnd;
            }
            return new TimeFlags(
                 SaturdayMorning: Overlaps(excuseStart, excuseEnd, saturdayStart, saturdayNoon),
                 SaturdayAfternoon: Overlaps(excuseStart, excuseEnd, saturdayNoon, sundayStart),
                 SundayMorning: Overlaps(excuseStart, excuseEnd, sundayStart, sundayNoon),
                 SundayAfternoon: Overlaps(excuseStart, excuseEnd, sundayNoon, sundayEnd.AddMinutes(1)) // ensure inclusive
            );
        }
        private void SetTimeFlags(TimeFlags TimeFlags, RefereeWithTimeOptions referee)
        {
            if(TimeFlags.SaturdayMorning)
                referee.isFreeSaturdayMorning = false;
            if (TimeFlags.SaturdayAfternoon)
                referee.isFreeSaturdayAfternoon = false;
            if (TimeFlags.SundayMorning)
                referee.isFreeSundayMorning = false;
            if (TimeFlags.SundayAfternoon)
                referee.isFreeSundayAfternoon = false;           
        }

    }
}
