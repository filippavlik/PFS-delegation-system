using AdminPartDevelop.Common;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Views.ViewModels;
using Azure.Core;
using Microsoft.Extensions.Primitives;

namespace AdminPartDevelop.Services.RefereeServices
{
    public interface IRefereeService
    {
        /// <summary>
        /// Processes a list of referees and their availability in relation to matches,excuses,transfers
        /// creating RefereeWithTimeOptions objects with availability data.
        /// </summary>
        /// <param name="listWithReferees">List of referee entities to process</param>
        /// <param name="listOfMatches">List of match view models containing match schedule information</param>
        /// <param name="listOfTransfers">List of transfer records for travel between locations</param>
        /// <param name="firstGameDay">The first day of the gaming weekend or match schedule</param>
        /// <returns>A ServiceResult containing a list of referees with their time availability options</returns>
        Task<ServiceResult<List<RefereeWithTimeOptions>>> AddRefereesTimeOptionsAsync(List<Referee> listWithReferees,List<MatchViewModel> listOfMatches,List<Transfer> listOfTransfers, DateOnly firstGameDay);
        /// <summary>
        /// Calculates and validates transfer routes when assigning a referee to a match,
        /// considering previous and next match locations, transportation methods, and time constraints.
        /// Creates transfer records for route planning between consecutive match assignments.
        /// </summary>
        /// <param name="matchToCheck">The target match where the referee is being assigned</param>
        /// <param name="refereeToAssign">Referee entity with time availability options and constraints</param>
        /// <param name="force">When true, bypasses time constraint validations and assigns referee regardless of conflicts</param>
        /// <returns>
        /// A ServiceResult containing a tuple with:
        /// - bool: True if assignment is valid/successful, false if time conflicts exist
        /// - string: Error message describing any time conflicts or constraints, empty if successful
        /// </returns>
        /// <remarks>
        /// This method performs several key operations:
        /// - Validates field coordinates are available for route calculation
        /// - Determines referee's transportation method (car vs public transport)
        /// - Calculates routes and travel times to/from previous and next matches
        /// - Creates transfer records in the database for approved routes
        /// - Returns detailed error messages for scheduling conflicts when force=false
        /// </remarks>
        Task<ServiceResult<TransportBetweenMatchesViewModel>> CalculateTransfersWhenAssigningAsync(Match matchToCheck, RefereeWithTimeOptions refereeToAssign, bool force);
        //new
        Task<ServiceResult<Tuple<Transfer?,Transfer?>>> CalculateHomeTransferWhenAssigningAsync(Match matchToCheck, RefereeWithTimeOptions referee);
        /// <summary>
        /// Processes a single referee's availability in relation to matches,excuses and transfers,
        /// creating a RefereeWithTimeOptions object with availability data.
        /// </summary>
        /// <param name="referee">The referee entity to process</param>
        /// <param name="listOfMatches">List of matches to check against referee availability</param>
        /// <param name="listOfTransfers">List of transfer records for travel between locations</param>
        /// <param name="firstGameDay">The first day of the  gaming weekend or match schedule</param>
        /// <returns>A ServiceResult containing a referee with their time availability options</returns>
        Task<ServiceResult<RefereeWithTimeOptions>> AddRefereeTimeOptionsAsync(Referee referee, List<Match> listOfMatches, List<Models.Transfer> listOfTransfers, DateOnly firstGameDay);
        /// <summary>
        /// Determines the date and time of the referee's next scheduled match after the specified match.
        /// </summary>
	/// <remarks>
	/// There need to be less than travellingMaximumPeriod + waitingTimeBeforeMatch to the next match to makes sense to calculate the transport time  
	/// </remarks>
        /// <param name="referee">The referee with their availability information</param>
        /// <param name="matchToCheck">The match being evaluated</param>
        /// <returns>A tuple containing the date/time of the next match and an optional match identifier</returns>
        ServiceResult<Tuple<DateTime,string?>> GetFirstNextMatchDateTime(RefereeWithTimeOptions referee,Match matchToCheck);
        /// <summary>
        /// Determines the date and time of the referee's previous scheduled match before the specified time.
        /// </summary>
	/// <remarks>                                                                                                           /// There need to be less than travellingMaximumPeriod + waitingTimeBeforeMatch from the end of previous match to the start of this match to makes sense to calculate the transport time                                                        /// </remarks>
        /// <param name="referee">The referee with their availability information</param>
        /// <param name="matchStart">The start time to check against</param>
        /// <returns>A tuple containing the date/time of the previous match and an optional match identifier</returns>
        ServiceResult<Tuple<DateTime,string?>> GetFirstPreviousMatchDateTime(RefereeWithTimeOptions referee, DateTime matchStart);
        /// <summary>
        /// Retrieves the geographical coordinates of the referee's location before a specified match.
        /// </summary>
	/// <remarks>
	/// We take only locations of matches which will happen in less than 4 hours(start) before the start of actuall match. 
	/// </remarks>
        /// <param name="listOfMatches">List of all matches to search through</param>
        /// <param name="startDateTime">Start time of the match to check</param>
        /// <returns>Coordinates (latitude, longitude) of the location before the match</returns>
        ServiceResult<Tuple<float, float>> GetLocationBeforeMatch(List<Models.Match> listOfMatches, DateTime startDateTime);
        /// <summary>
        /// Retrieves the geographical coordinates of the referee's location after a specified match.
        /// </summary>
	/// <remarks>
        /// We take only locations of matches which will happen in less than 4 hours(start) after the start of actuall match.           /// </remarks>
        /// <param name="listOfMatches">List of all matches to search through</param>
        /// <param name="startDateTime">Start time of the match to check</param>
        /// <returns>Coordinates (latitude, longitude) of the location after the match</returns>
        ServiceResult<Tuple<float, float>> GetLocationAfterMatch(List<Models.Match> listOfMatches, DateTime startDateTime);
        //New
        /// <summary>
        /// Retrieves the geographical coordinates of the referee's home location .
        /// </summary>
        /// <param name="refereeId">Id of the referee</param>
        /// <returns>Coordinates (latitude, longitude) of the home location </returns>
        Task<ServiceResult<Tuple<float, float>>> GetHomeLocation(int refereeId);
        /// <summary>
        /// Creates a dictionary mapping referee IDs to their names for easy reference.
        /// </summary>
        /// <param name="listOfReferees">List of referee entities</param>
        /// <returns>Dictionary with referee IDs as keys and full names as values</returns>
        ServiceResult<Dictionary<int, string>> GetRefereeDictionary(List<Referee> listOfReferees);
        /// <summary>
        /// Sorting referees in order their league level, sort the specified lists by surname 
        /// </summary>
        /// <param name="listOfReferees">The list with all referees.</param>
        /// <returns>6 lists of refeerees sorted by their league level.</returns>
        ServiceResult<Dictionary<int, List<RefereeWithTimeOptions>>> SortRefereesByLeague(List<RefereeWithTimeOptions> listOfReferees);
        ServiceResult<Tuple<bool,string>> DoesExceedTheMaximumMatchLimit(RefereeWithTimeOptions referee, DateTime matchTime);

        /// <summary>
        /// Calculates points for referees based on previous and current match delegations and travel distances.
        /// </summary>
        /// <remarks>
        /// <br/> check veto for both teams , if only one of these exists -80 points for referee (max -80)
        /// <br/> the number of matches delegated as a referee/assistant for the teams where team is as a home team 
        /// to 2 matches -3 for each, third match is for -5 , four and more are for additional -10 (max -31)
        /// <br/> the number of matches delegated as a referee/assistant for the teams where team is as a away team             /// to 2 matches -2 for each, third match is for -3 , four and more are for additional -5 (max -17)
        /// <br/> the distance of field in order to previous match or residence, we consider max air distance 35 km in Prague competitions so for every 5 km -3 points (max -21)
        /// <br/> the number of matches this gaming weekend , for every match -2 points as assistant -3 as referee (max -18)
        /// </remarks>
        /// <param name="refereeIds">List of referee IDs to calculate points for</param>
        /// <param name="homeMatches">Result of previous method with counter of home matches with referee delegation for every referee</param>
        /// <param name="awayMatches">Result of previous method with counter of away matches with referee delegation for every referee</param>
        /// <param name="totalMatches">Result of previous method with counter of matches currently delegated in this playing weekend</param>
        /// <param name="distanceDictionary">Dictionary of distance in average between previous/next field and actuall match field for every referee</param>
        /// <returns>Dictionary mapping referee IDs to tuples containing points and additional text info about penalties for admins</returns>
        ServiceResult<Dictionary<int, Tuple<int, string>>> CalculatePointsForReferees(List<int> refereeIds, Dictionary<int, bool> canRefereeTheCompetition, Dictionary<int, bool> isFreeDuringMatch,Dictionary<int,int> ratingOfReferees, List<RefereesTeamsMatchesResponseDto> homeMatches, List<RefereesTeamsMatchesResponseDto> awayMatches, List<RefereesMatchesResponseDto> totalMatches, Dictionary<int, int> distanceDictionary);
        /// <summary>
        /// Checks if a referee is available at the time of a specified match. We takes into consideration excuses and already delegated matches
        /// </summary>
        /// <param name="refereeToCheck">The referee to check availability for</param>
        /// <param name="matchToCheck">The match to check against</param>
        /// <returns>True if the referee is available, otherwise false</returns>
        ServiceResult<bool> CheckTimeAvailabilityOfReferee(RefereeWithTimeOptions refereeToCheck, Match matchToCheck);
        /// <summary>
        /// Checks if a referee has a car available (he added vehicle slot via referee part) in the time of specified match.
        /// </summary>
        /// <param name="referee">The referee to check car availability for</param>
        /// <param name="matchToCheck">The match to check against</param>
        /// <returns>
        /// Nullable bool: true if car is available, false if not, null if no matching slot is found.
        /// </returns>
        ServiceResult<bool?> CheckCarAvailabilityOfReferee(RefereeWithTimeOptions referee, Models.Match matchToCheck);
        /// <summary>
        /// Performs a complex check of referee availability considering transfer time between matches.
        /// </summary>
        /// <param name="startOrEndOfConnectedMatch">Start or end time of a connected match</param>
        /// <param name="connectedMatchId">ID of a match connected to the one being checked</param>
        /// <param name="matchToCheck">The match being evaluated</param>
        /// <param name="transferLength">Time required for transfer between locations</param>
        /// <param name="refereeId">ID of the referee being checked</param>
        /// <param name="isPreMatch">Indicates if connected match is before (true) or after(false) evaluated match</param>
        /// <param name="hasCar">Indicates if referee has access to a car</param>
        /// <returns>
        /// A tuple containing:
        /// - bool: true if the referee is available, false if not,
        /// - double: number of minutes of overlap or buffer,
        /// - Transfer: transfer object containing timing and metadata.
        /// </returns>
        ServiceResult<Tuple<bool, double,Transfer>> CheckTimeAvailabilityWithTransferOfReferee(DateTime startOrEndOfConnectedMatch,string connectedMatchId, Match matchToCheck, int transferLength,int refereeId, bool isPreMatch,bool hasCar);



       

    }
}
