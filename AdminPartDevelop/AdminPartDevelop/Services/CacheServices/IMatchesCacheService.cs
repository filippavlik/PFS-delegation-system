using AdminPartDevelop.Common;
using AdminPartDevelop.Models;

namespace AdminPartDevelop.Services.CacheServices
{
    public interface IMatchesCacheService
    {
            /// <summary>
            /// Gets all matches from cache. If cache is empty, loads from database.
            /// </summary>
            Task<ServiceResult<List<Match>>> GetMatchesFromCacheAsync();

            /// <summary>
            /// Refreshes a specific match in the cache by fetching latest data from database.
            /// </summary>
            /// <param name="matchId">The match ID to refresh</param>
            Task<ServiceResult<List<Match>>> UploadRefreshedMatchToCacheAsync(string matchId);

            /// <summary>
            /// Clears the entire match cache, forcing next read to fetch from database.
            /// </summary>
            void ClearCache();

            /// <summary>
            /// Checks if the cache contains data.
            /// </summary>
            bool IsCachePopulated();
    }
    
}
