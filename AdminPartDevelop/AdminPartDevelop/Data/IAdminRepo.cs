using AdminPartDevelop.Common;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Models;
using AdminPartDevelop.Views.ViewModels;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace AdminPartDevelop.Data
{
    /// <summary>
    /// Interface for managing data within admin-database in the repository.
    /// </summary>
    public interface IAdminRepo
    {
        /// <summary>
        /// Adds a <paramref name="listOfMatches"/> to the database.
        /// </summary>
        /// <remarks>
        /// The matches into database uploads only main admin at the beggining of gaming weekend/period.
        /// </remarks>
        /// <param name="listOfMatches">The list of match entities to add.</param>
        /// <returns>A repository response indicating success when added or failure with an appropriate message.</returns>
        Task<RepositoryResponse> AddMatchesAsync(List<Match> listOfMatches);

        /// <summary>
        /// Adds a <paramref name="vetoToAdd"/> to the database.
        /// </summary>
        /// <remarks>
        /// The vetoes into database upload all admins at the beggining of the competitions or during them.
        /// </remarks>
        /// <param name="vetoToAdd">The veto entity to add including id or referee.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> AddVeto(Veto vetoToAdd);

        /// <summary>
        /// Adds a  <paramref name="transfer"/> record to the database.
        /// </summary>
        /// <remarks>
        /// The transfers into database are saved automatically based on calculations from RouteByBus/CarPlanner.cs
        /// </remarks>
        /// <param name="transfer">The transfer entity to add including id of referee and matches whose are connected with this transfer.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> AddTransfer(Transfer transfer);

        /// <summary>
        /// Adds a new file containing previous delegation information.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="amountOfMatches">The number of matches in the file.</param>
        /// <param name="matchesFrom">The start date of matches in the file (optional).</param>
        /// <param name="matchesTo">The end date of matches in the file (optional).</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        RepositoryResponse AddNewFilePreviousDelegation(string filePath, int amountOfMatches, DateOnly? matchesFrom, DateOnly? matchesTo);

        /// <summary>
        /// Assigns a referee to a match with a specified role.
        /// </summary>
        /// <param name="refereeId">The ID of the referee to assign.</param>
        /// <param name="matchId">The ID of the match.</param>
        /// <param name="role">The role ID of the referee in the match.</param>
        /// <param name="user">The user who is trying to do the action.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> AddRefereeToTheMatch(int refereeId, string matchId, int role, string user);

        /// <summary>
        /// Retrieves a match by its ID.
        /// </summary>
        /// <param name="id">The ID of the match to retrieve.</param>
        /// <returns>A repository result containing the match if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<Models.Match>> GetMatchByIdAsync(string id);

        /// <summary>
        /// Retrieves all matches whose are in the system and will be played in the future.
        /// </summary>
        /// <remarks>
        /// To every match add also detailed referees informations (name) ,teams,competition,field names for easier visualizing.
        /// </remarks>
        /// <param name="nameOfReferees">A dictionary mapping referee IDs to their names.</param>
        /// <returns>A repository result containing a list of match view models if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<MatchViewModel>>> GetMatchesAsync(Dictionary<int, string> nameOfReferees);
        /// <summary>
        /// Convert matches to match view models.
        /// </summary>
        /// <remarks>
        /// To every match add also detailed referees informations (name) ,teams,competition,field names for easier visualizing.
        /// </remarks>
        /// <param name="listOfMatches">THe list of actuall matches.</param>
        /// <param name="dictNameOfReferees">A dictionary mapping referee IDs to their names.</param>
        /// <returns>A repository result containing a list of match view models if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<MatchViewModel>>> ConvertMatchesToViewModels(List<Models.Match> listOfMatches,
         Dictionary<int, string> dictNameOfReferees);


        /// <summary>
        /// Retrieves matches within a date range with referee information.
        /// </summary>
        /// <param name="dictNameOfReferees">A dictionary mapping referee IDs to their names.</param>
        /// <param name="startDate">The start date for filtering matches.</param>
        /// <param name="endDate">The end date for filtering matches.</param>
        /// <returns>A repository result containing a list of match view models if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<MatchViewModel>>> GetMatchesByDateAsync(Dictionary<int, string> dictNameOfReferees, DateTime startDate, DateTime endDate);
        /// <summary>                                                                                                           /// Retrieves matches within a competition with referee information.                                                     /// </summary>                                                                                                          /// <param name="dictNameOfReferees">A dictionary mapping referee IDs to their names.</param>                           /// <param name="competitionName">The name of competition , we want only matches within this competition.</param>                                               
        /// <returns>A repository result containing a list of match view models if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<MatchViewModel>>> GetMatchesByCompetitionAsync(Dictionary<int, string> dictNameOfReferees, string competitionName);
        /// <summary>
        /// Retrieves all matches whose are in the system and will be played in the future.
        /// </summary>
        /// <remarks>
        /// Method is similar to GetMatchesAsync() , but it is not used for displaying matches , but rather for extracting the informations about referees of refreshing cache. 		
        /// </remarks>
        /// <returns>A repository result containing a list of matches if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Models.Match>>> GetPureMatchesAsync();

        /// <summary>
        /// Retrieves all matches that have not been played yet.
        /// </summary>
        /// <returns>A repository result containing a list of match entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Models.Match>>> GetPureNotPlayedMatchesAsync();

        /// <summary>
        /// Retrieves all vetoes for a specific referee.
        /// </summary>
        /// <param name="refereeId">The ID of the referee.</param>
        /// <returns>A repository result containing a list of veto entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Models.Veto>>> GetRefereesVetoesAsync(int refereeId);
        Task<RepositoryResult<List<(Competition,bool)>>> GetCustomCompetitionsRulesForReferee(int refereeId,int refereeLeague);
        Task<RepositoryResult<bool>> DoesCustomCompetitionRuleAlreadyExists(int refereeId,int refereeLevel, string competitionId, bool wantsToAdd);
        Task<RepositoryResponse> AddRemoveCustomRule(int refereeId, string competitionId, bool wantsToAdd);


        /// <summary>
        /// Retrieves all transfers for a specific referee.
        /// </summary>
        /// <remarks>
        /// Used in delegations, or when displaying time schedule of referee.
        /// </remarks>
        /// <param name="refereeId">The ID of the referee.</param>
        /// <returns>A repository result containing a list of transfer entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Models.Transfer>>> GetRefereesTransfersAsync(int refereeId);

        /// <summary>
        /// Gets the start date of the gaming block (f.e weekend).
        /// </summary>
        /// <returns>A repository result containing the start date if successful, or an error message if the operation fails.</returns>
        RepositoryResult<DateOnly> GetStartGameDate();

        /// <summary>
        /// Retrieves all matches for a specific team and competition, but they must be already labelled as played.
        /// </summary>
        /// <remarks>
        /// Used in checking how many times referee this season refereed this specific team.
        /// </remarks>
        /// <param name="teamId">The ID of the team.</param>
        /// <param name="competitionId">The ID of the competition.</param>
        /// <returns>A repository result containing a list of match entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Models.Match>>> GetTeamsPureMatchesAsync(string teamId, string competitionId);

        /// <summary>
        /// Searches for teams based on input text.
        /// </summary>
        /// <remarks>
        //  Used in help for admins when adding veto team , we use case-insensitive partial matching.
        /// </remarks>
        /// <example>
        ///		GetTeamsByInput("Oko") --> TJ Sokol Cholupice, z.s., FK Loko Vltavín, z.s.
        /// </example>
        /// <param name="input">The search text.</param>
        /// <returns>A repository result containing a list of team entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Models.Team>>> GetTeamsByInput(string input);

        /// <summary>
        /// Gets an existing team or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="id">The ID of the team.</param>
        /// <param name="name">The name of the team.</param>
        /// <returns>A repository result containing the team entity if successful, or an error message if the operation fails.</returns>
        RepositoryResult<Team> GetOrSaveTheTeam(string id, string name);

        /// <summary>
        /// Retrieves all transfers occurring within a specific game weekend.
        /// </summary>
        /// <remarks>   
        /// Used when adding time options to all referees in the start of admin part , we want to limit the number of transfer for only relevant ones , gaming weekend is ussually 3 days , so it is transfers from startDay to startDay+4 days  
        /// </remarks>
        /// <param name="startDayOfWeekend">The start date of the weekend.</param>
        /// <returns>A repository result containing a list of transfer entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Models.Transfer>>> GetTransfersWithinGameWeekend(DateTime startDayOfWeekend);

        /// <summary>
        /// Retrieves all fields from the database.
        /// </summary>
        /// <returns>A repository result containing a list of field entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Field>>> GetFields();

        /// <summary>
        /// Gets an existing field or creates a new one if it doesn't exist, so we can add location to the field.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>A repository result containing the field entity if successful, or an error message if the operation fails.</returns>
        RepositoryResult<Field> GetOrSaveTheField(string fieldName);

        /// <summary>
        /// Retrieves all competitions from the database.
        /// </summary>
        /// <returns>A repository result containing a list of competition entities if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<Competition>>> GetCompetitions();

        /// <summary>
        /// Retrieves files containing previous delegation information.
        /// </summary>
        /// <returns>A repository result containing a list of files with previous matches if successful, or an error message if the operation fails.</returns>
        Task<RepositoryResult<List<FilesPreviousDelegation>>> GetFilesWithPreviousMatchesAsync();

        /// <summary>
        /// Updates a match in the database.
        /// </summary>
        /// <param name="match">The updated match entity.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> UpdateMatchAsync(Models.Match match);
        /// <summary>
        /// Updates a matches date in the database.
        /// </summary>
        /// <param name="match">The if of updated entity.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> UpdateMatchDateAsync(DateOnly newDate, TimeOnly newTime, string matchId, string user);

        /// <summary>
        /// Updates the pre-match relationship for a match.
        /// </summary>
        /// <param name="existingMatch">The existing match entity.</param>
        /// <param name="newPreMatchId">The ID of the new pre-match to link.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> UpdatePreMatchRelationship(Models.Match existingMatch, string newPreMatchId);

        /// <summary>
        /// Updates the post-match relationship for a match.
        /// </summary>
        /// <param name="existingMatch">The existing match entity.</param>
        /// <param name="newPostMatchId">The ID of the new post-match to link.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> UpdatePostMatchRelationship(Models.Match existingMatch, string newPostMatchId);

        /// <summary>
        /// Updates multiple matches in the database.
        /// </summary>
        /// <param name="listOfMatches">The list of updated match entities.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> UpdateMatchesAsync(List<Match> listOfMatches);

        /// <summary>
        /// Toggles the lock status of a match.
        /// </summary>
        /// <param name="id">The ID of the match to update.</param>
        /// <param name="user">The user who is trying to do the action.</param>
        /// <returns>A repository result containing a boolean indicating success and the new lock status, or an error message if the operation fails.</returns>
        Task<RepositoryResult<bool>> UpdateMatchLockAsync(string id, string user);

        /// <summary>
        /// Marks a match as played.
        /// </summary>
        /// <param name="id">The ID of the match to update.</param>
        /// <param name="user">The user who is trying to do the action.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> UpdateMatchPlayedAsync(string id, string user);

        /// <summary>
        /// Links matches with referees and updates the database.
        /// </summary>
        /// <remarks> 
        /// Used in proccess of saving played matches to server , we update the matches whose are in the database already                                   
        /// </remarks>
        /// <param name="listOfMatches">The list of filled match DTOs.</param>
        /// <param name="refereeDict">A dictionary mapping referee IDs to their internal IDs.</param>
        /// <param name="filePath">The path to the file containing match information.</param>
        /// <param name="user">The user who is trying to do the action.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> TieAndUpdateTheMatchesAsync(List<FilledMatchDto> listOfMatches, Dictionary<string, int> refereeDict, string filePath, string user);

        /// <summary>
        /// Updates multiple fields in the database.
        /// </summary>
        /// <param name="listOfFields">The list of filled field DTOs.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> UpdateFieldsAsync(List<FilledFieldDto> listOfFields);

        /// <summary>
        /// Adds a <paramref name="fieldToAdd"/> to the database.
        /// </summary>
        /// <param name="fieldToAdd">The field entity with unique id.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> AddField(Field fieldToAdd);
        /// <summary>
        /// Updates the properties of an existing field.
        /// </summary>
        /// <param name="fieldToUpdate">The field update DTO containing the new values.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        RepositoryResponse UpdateExistingField(FieldToUpdateDto fieldToUpdate);
        /// <summary>
        /// Adds a <paramref name="competitionToAdd"/> to the database.
        /// </summary>
        /// <param name="competitionToAdd">The competition entity with unique id.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> AddCompetition(Competition competitionToAdd);

        /// <summary>
        /// Updates the properties of an existing competition.
        /// </summary>
        /// <param name="competitionToUpdate">The competition update DTO containing the new values.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        RepositoryResponse UpdateExistingCompetition(CompetitionToUpdateDto competitionToUpdate);
        /// <summary>
        /// Updates the note for an existing veto.
        /// </summary>
        /// <param name="id">The ID of the veto to update.</param>
        /// <param name="note">The new note text.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        RepositoryResponse UpdateExistingVeto(int id, string note);

        /// <summary>
        /// Sets the start date of the game season.
        /// </summary>
        /// <param name="date">The start date to set.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        RepositoryResponse UploadStartGameDate(DateOnly date);

        /// <summary>
        /// Deletes a veto from the database.
        /// </summary>
        /// <param name="id">The ID of the veto to delete.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        RepositoryResponse DeleteVeto(int id);

        /// <summary>
        /// Removes a referee from a match.
        /// </summary>
        /// <param name="refereeId">The ID of the referee to remove.</param>
        /// <param name="matchId">The ID of the match.</param>
        /// <param name="user">The user who is trying to do the action.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> RemoveRefereeFromTheMatch(int refereeId, string matchId, string user);

        /// <summary>
        /// Removes transfers connected to a specific referee and match.
        /// </summary>
        /// <param name="refereeId">The ID of the referee.</param>
        /// <param name="matchId">The ID of the match.</param>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> RemoveTransfersConnectedTo(int refereeId, string matchId);
        /// <summary>
        /// Get matches whose are connected with matchId .
        /// </summary>
        /// <param name="refereeId">The ID of the referee.</param>
        /// <param name="matchId">The ID of the match.</param>
        /// <returns>A repository result of Tuple where first item is preMatch id and second is PostMatch id.</returns>
        Task<RepositoryResult<Tuple<Match?,Match?>>> GetConnectedMatchesByTransfer(int refereeId, string matchId);
        Task<RepositoryResponse> RemoveInGoingTransfers(int refereeId, string matchId);
        Task<RepositoryResponse> RemoveOutGoingTransfers(int refereeId, string matchId);


        /// <summary>
        /// Checks if a competition exists in the database.
        /// </summary>
        /// <param name="idOfCompetition">The ID of the competition to check.</param>
        /// <returns>A repository result containing the competition if it exists, or an error message if it doesn't.</returns>
        RepositoryResult<Competition> DoesCompetitionExist(string idOfCompetition);

        /// <summary>
        /// Checks if a match exists in the database.
        /// </summary>
        /// <param name="matchId">The ID of the match to check.</param>
        /// <returns>A repository response indicating whether the match exists.</returns>
        RepositoryResponse DoesMatchExists(string matchId);

        /// <summary>
        /// Checks if a veto exists for a specific match and referee.
        /// </summary>
        /// <param name="matchId">The ID of the match.</param>
        /// <param name="refereeId">The ID of the referee.</param>
        /// <returns>A repository result containing a boolean indicating whether the veto exists, or an error message if the operation fails.</returns>
        Task<RepositoryResult<bool>> DoesVetoExist(string matchId, int refereeId);

        /// <summary>
        /// Checks if a veto exists for a specific team, competition, and referee.
        /// </summary>
        /// <param name="teamId">The ID of the team.</param>
        /// <param name="competitionId">The ID of the competition.</param>
        /// <param name="refereeId">The ID of the referee.</param>
        /// <returns>A repository result containing a boolean indicating whether the veto exists, or an error message if the operation fails.</returns>
        RepositoryResult<bool> DoesVetoExistForTeam(string teamId, string competitionId, int refereeId);
    }

}
