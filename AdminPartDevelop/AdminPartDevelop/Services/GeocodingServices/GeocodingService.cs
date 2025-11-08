using AdminPartDevelop.Common;
using AdminPartDevelop.Views.ViewModels;
using Aspose.Cells;
using Nest;
using System.Globalization;
using System.Text.Json;

namespace AdminPartDevelop.Services.GeocodingServices
{
    /// <summary>
    /// Implements the IGeocodingService interface to suggest user the valid address and translate this selected address to latitude and longtitude
    /// via the Mapy.cz routing API.
    /// </summary>
    public class GeocodingService : IGeocodingService
    {
        private readonly ILogger<GeocodingService> _logger;    
        private readonly HttpClient _httpClient;                // HTTP client for making API requests
        private readonly string _apiKey;                        // API key for authenticating with Mapy.cz
        private const string BaseUrl = "https://api.mapy.com/v1/suggest";  // Base URL for the geocoding API
        private int returnedRecordsLimit = 5;

        /// <summary>
        /// Constructor initializes the geocoding service with necessary dependencies
        /// </summary>
        /// <param name="logger">Logger for recording events and errors</param>
        /// <param name="apiKey">API key for Mapy.cz service</param>
        /// <exception cref="ArgumentNullException">Thrown when apiKey is null</exception>
        public GeocodingService(
            ILogger<GeocodingService> logger,
            IHttpClientFactory httpClientFactory,
            string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("MapyGeocodingClient");
        }

        /// <summary>
        /// Suggest possible adddresses for user based on his output
        /// </summary>
        /// <param name="query">Users input</param>
        /// <returns>
        /// A ServiceResult containing the valid address with informations about its latitude and longtitude
        /// </returns>
        public async Task<ServiceResult<List<SuggestedAddressViewModel>>> SuggestAddress(string input)
        {
            try
            {            
                // Construct the API request URL with all necessary parameters
                var url = $"{BaseUrl}?" +
                    $"apikey={_apiKey}" +
                    $"&lang=cs" +                         
                    $"&query={input}" + 
                    $"&limit={returnedRecordsLimit}" +
                    $"&type=regional" +
                    $"&type=poi" +
                    $"&locality=cz";                   

                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    return ServiceResult<List<SuggestedAddressViewModel>>.Failure(
                        "Zkontrolujte zadanou adresu a její formát, případne zadejte manuálne.");
                }
                _logger.LogInformation(url);
                response.EnsureSuccessStatusCode();

                List<SuggestedAddressViewModel> returnedAddresses = new List<SuggestedAddressViewModel>();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                using (JsonDocument document = JsonDocument.Parse(jsonResponse))
                {
                    var root = document.RootElement;

                    // Get "items" array
                    var items = root.GetProperty("items");

                    foreach (var item in items.EnumerateArray())
                    {
                        string name = item.GetProperty("name").GetString();
                        string location = item.GetProperty("location").GetString();

                        float lat = (float)item.GetProperty("position").GetProperty("lat").GetDouble();
                        float lon = (float)item.GetProperty("position").GetProperty("lon").GetDouble();
                        returnedAddresses.Add(new SuggestedAddressViewModel(name,location, lat, lon));
                    }
                    
                }
                return ServiceResult<List<SuggestedAddressViewModel>>.Success(returnedAddresses);

            }
            catch (HttpRequestException ex)
            {
                // Handle network or API errors
                _logger.LogError(ex, "[SuggestAddress] Error getting addresses");
                return ServiceResult<List<SuggestedAddressViewModel>>.Failure("Nepodařilo se získat adresy!"); 
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors
                _logger.LogError(ex, "[SuggestAddress] Error getting addresses(parsing)");
                return ServiceResult<List<SuggestedAddressViewModel>>.Failure("Nepodařilo se získat adresy (problémy s parsováním)!");
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                _logger.LogError(ex, "[SuggestAddress] Error getting addresses (unknown error)");
                return ServiceResult<List<SuggestedAddressViewModel>>.Failure("Nepodařilo se získat adresy (neznáma chyba)!");
            }
        }
    }
}
