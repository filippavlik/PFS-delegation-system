using AdminPartDevelop.Common;
using AdminPartDevelop.DTOs;
using AdminPartDevelop.Models;

namespace AdminPartDevelop.Data
{
    /// <summary>
    /// Interface for managing referee data from referee-database in the repository.
    /// </summary>
    public interface IRefereeRepo
    {
        /// <summary>
        /// Retrieves all excuses from the database.
        /// </summary>
        /// <returns>
        /// A repository result containing a list of excuses if successful,
        /// or an error message if the operation fails.
        /// </returns>
        Task<RepositoryResult<List<Excuse>>> GetExcusesAsync();
        /// <summary>
        /// Retrieves all referees from the database, including their vehicle slots and excuses.
        /// </summary>
        /// <returns>
        /// A repository result containing a list of referees if successful,
        /// or an error message if the operation fails.
        /// </returns>
        Task<RepositoryResult<List<Referee>>> GetRefereesAsync();
        /// <summary>
        /// Retrieves a specific referee by their ID, including their excuses and vehicle slots.
        /// </summary>
        /// <param name="id">The ID of the referee to retrieve.</param>
        /// <returns>
        /// A repository result containing the referee if successful,
        /// or an error message if the referee wasn't found or the operation fails.
        /// </returns>
        Task<RepositoryResult<Referee>> GetRefereeByIdAsync(int id);
        /// <summary>
        /// Maps FACR IDs from a list of matches to internal referee IDs in the database.
        /// If a referee with the FACR ID is not found, attempts to find them by name and surname.
        /// </summary>
        /// <param name="listOfMatches">A list of filled match DTOs containing referee information.</param>
        /// <returns>
        /// A repository result containing a dictionary mapping FACR IDs to internal referee IDs if successful,
        /// or an error message if the operation fails.
        /// </returns>
        Task<RepositoryResult<Dictionary<string, int>>> GetRefereeIdsFromFacrIdOrNameAsync(List<FilledMatchDto> listOfMatches);
        /// <summary>
        /// Retrieves a mapping of referee IDs to their full names and FACR IDs.
        /// </summary>
        /// <returns>
        /// A repository result containing a dictionary where each key is a referee ID,
        /// and each value is a tuple containing the referee's full name and FACR ID if successful,
        /// or an error message if the operation fails.
        /// </returns>
        Task<RepositoryResult<Dictionary<int, Tuple<string, string>>>> GetRefereeRealNameAndFacrIdById();
        /// <summary>
        /// Adds a new referee to the database after checking for duplicates by name and surname.
        /// </summary>
        /// <param name="referee">The referee entity to add.</param>
        /// <returns>
        /// A repository response indicating success or failure with an appropriate message.
        /// Returns failure if a referee with the same name and surname already exists.
        /// </returns>
        Task<RepositoryResponse> AddRefereeAsync(Referee referee);
	        /// <summary>
        /// Saves a list of excuses for a specific referee.
        /// </summary>
        /// <param name="excuses">The list of excuses to be saved</param>
        /// <returns>A status message indicating whether the excuses were successfully saved or if there was an error</returns>
        /// <returns>A repository response indicating success or failure with an appropriate message.</returns>
        Task<RepositoryResponse> AddManualExcuses(List<Excuse> excuses);
        /// <summary>
        /// Updates an existing referee's information in the database.
        /// </summary>
        /// <param name="id">The ID of the referee to update.</param>
        /// <param name="referee">The updated referee information.</param>
        /// <returns>
        /// A repository response indicating success or failure with an appropriate message.
        /// </returns>
        Task<RepositoryResponse> UpdateRefereeAsync(int id,RefereeAddRequest referee);
        /// <summary>
        /// Updates multiple referees based on a dictionary of name/surname pairs and referee DTOs.
        /// Creates new referees if they don't exist in the database.
        /// </summary>
        /// <param name="referees">
        /// A dictionary where each key is a KeyValuePair of name and surname,
        /// and each value is an object implementing IRefereeDto with referee information.
        /// </param>
        /// <returns>
        /// A repository response indicating success or failure with an appropriate message.
        /// </returns>
        Task<RepositoryResponse> UpdateRefereesAsync(Dictionary<KeyValuePair<string, string>, IRefereeDto> referees);


    }
}
