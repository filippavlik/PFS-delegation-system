using AdminPartDevelop.Common;
using AdminPartDevelop.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AdminPartDevelop.Services.CacheServices
{
    public class MatchesCacheService : IMatchesCacheService
    {
            private readonly IMemoryCache _memoryCache;
            private readonly Data.IAdminRepo _adminRepo;
            private readonly ILogger<MatchesCacheService> _logger;
            private const string MatchesCacheKey = "AppMatches";
            private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

            public MatchesCacheService(
                IMemoryCache memoryCache,
                Data.IAdminRepo adminRepo,
                ILogger<MatchesCacheService> logger)
            {
                _memoryCache = memoryCache;
                _adminRepo = adminRepo;
                _logger = logger;
            }

            public async Task<ServiceResult<List<Match>>> GetMatchesFromCacheAsync()
            {
                try
                {
                    if (!_memoryCache.TryGetValue(MatchesCacheKey, out List<Match> cachedMatches))
                    {
                        _logger.LogInformation("Cache miss - loading matches from database");

                        var result = await _adminRepo.GetPureMatchesAsync();
                        if (!result.IsSuccess)
                        {
                            return ServiceResult<List<Match>>.Failure("Chyba při načítání zápasů z databáze.");
                        }

                        cachedMatches = result.Data;
                        _memoryCache.Set(MatchesCacheKey, cachedMatches, CacheDuration);

                        _logger.LogInformation($"Loaded {cachedMatches.Count} matches into cache");
                    }

                    return ServiceResult<List<Match>>.Success(cachedMatches);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GetMatchesFromCacheAsync] Error loading matches from cache");
                    return ServiceResult<List<Match>>.Failure("Nastala chyba při získávání zápasů z cache.");
                }
            }

            public async Task<ServiceResult<List<Match>>> UploadRefreshedMatchToCacheAsync(string matchId)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(matchId))
                    {
                        return ServiceResult<List<Match>>.Failure("Match ID nesmí být prázdný.");
                    }

                    // Get current cache or initialize it
                    if (!_memoryCache.TryGetValue(MatchesCacheKey, out List<Match> cachedMatches))
                    {
                        _logger.LogInformation("Cache empty - initializing cache before update");

                        var initResult = await _adminRepo.GetPureMatchesAsync();
                        if (!initResult.IsSuccess)
                        {
                            return ServiceResult<List<Match>>.Failure("Chyba při inicializaci cache.");
                        }

                        cachedMatches = initResult.Data;
                        _memoryCache.Set(MatchesCacheKey, cachedMatches, CacheDuration);
                        return ServiceResult<List<Match>>.Success(cachedMatches);
                    }

                    // Find and update the specific match
                    var matchIndex = cachedMatches.FindIndex(m => m.MatchId == matchId);

                    if (matchIndex != -1)
                    {
                        var freshMatchResult = await _adminRepo.GetMatchByIdAsync(matchId);
                        if (!freshMatchResult.IsSuccess)
                        {
                            return ServiceResult<List<Match>>.Failure($"Chyba při získávání zápasu {matchId} z databáze.");
                        }

                        cachedMatches[matchIndex] = freshMatchResult.Data;
                        _memoryCache.Set(MatchesCacheKey, cachedMatches, CacheDuration);

                        _logger.LogInformation($"Updated match {matchId} in cache");
                    }
                    else
                    {
                        _logger.LogWarning($"Match {matchId} not found in cache, may be newly added");

                        // Match might be new, refresh entire cache
                        var refreshResult = await _adminRepo.GetPureMatchesAsync();
                        if (refreshResult.IsSuccess)
                        {
                            cachedMatches = refreshResult.Data;
                            _memoryCache.Set(MatchesCacheKey, cachedMatches, CacheDuration);
                        }
                    }

                    return ServiceResult<List<Match>>.Success(cachedMatches);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[UploadRefreshedMatchToCacheAsync] Error updating match {matchId} in cache");
                    return ServiceResult<List<Match>>.Failure("Nastala chyba při aktualizaci zápasu v cache.");
                }
            }

            public void ClearCache()
            {
                try
                {
                    _memoryCache.Remove(MatchesCacheKey);
                    _logger.LogInformation("Match cache cleared");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ClearCache] Error clearing match cache");
                }
            }

            public bool IsCachePopulated()
            {
                return _memoryCache.TryGetValue(MatchesCacheKey, out _);
            }
        }
}

