using AdminPart.Common;
using System.Globalization;
using System.Text.Json;

namespace AdminPart.Services.RouteServices
{
    /// <summary>
    /// Implements the IRoutePlanner interface to calculate routes using car transportation
    /// via the Mapy.cz routing API.
    /// </summary>
    public class RouteByCarPlanner : IRouteCarPlanner
    {
        private readonly ILogger<RouteByCarPlanner> _logger;    
        private readonly HttpClient _httpClient;                // HTTP client for making API requests
        private readonly string _apiKey;                        // API key for authenticating with Mapy.cz
        private const string BaseUrl = "https://api.mapy.cz/v1/routing/route";  // Base URL for the routing API

        /// <summary>
        /// Constructor initializes the route planner with necessary dependencies
        /// </summary>
        /// <param name="logger">Logger for recording events and errors</param>
        /// <param name="apiKey">API key for Mapy.cz service</param>
        /// <exception cref="ArgumentNullException">Thrown when apiKey is null</exception>
        public RouteByCarPlanner(
            ILogger<RouteByCarPlanner> logger,
            IHttpClientFactory httpClientFactory,
            string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("MapyClient");
        }

        /// <summary>
        /// Calculates a car route distance and length between two geographic coordinates using the Mapy.cz API
        /// </summary>
        /// <param name="startLatitude">Starting point latitude</param>
        /// <param name="startLongtitude">Starting point longitude</param>
        /// <param name="endLatitude">Destination latitude</param>
        /// <param name="endLongtitude">Destination longitude</param>
        /// <param name="departureTime">Optional departure time (not used in current implementation)</param>
        /// <returns>
        /// A ServiceResult containing a Tuple with distance in kilometers and duration in minutes,
        /// or an error message if the calculation fails
        /// </returns>
        public async Task<ServiceResult<Tuple<int, int>>> CalculateRoute(float startLatitude, float startLongtitude, float endLatitude, float endLongtitude, DateTime? departureTime = null)
        {
            try
            {
                // Format coordinates with invariant culture to ensure decimal points
                string startLat = startLatitude.ToString(CultureInfo.InvariantCulture);
                string startLon = startLongtitude.ToString(CultureInfo.InvariantCulture);
                string endLat = endLatitude.ToString(CultureInfo.InvariantCulture);
                string endLon = endLongtitude.ToString(CultureInfo.InvariantCulture);

                // Construct the API request URL with all necessary parameters
                var url = $"{BaseUrl}?" +
                    $"apikey={_apiKey}" +
                    $"&lang=cs" +                         
                    $"&start={startLon}&start={startLat}" + 
                    $"&end={endLon}&end={endLat}" +       
                    $"&routeType=car_fast" +               
                    $"&avoidToll=false";                   

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); 

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using (JsonDocument document = JsonDocument.Parse(jsonResponse))
                {
                    var root = document.RootElement;

                    int distanceMeters = root.GetProperty("length").GetInt32();
                    int durationSeconds = root.GetProperty("duration").GetInt32();

                    int distanceKm = distanceMeters / 1000;        
                    int durationMinutes = durationSeconds / 60;      

                    return ServiceResult<Tuple<int, int>>.Success(new Tuple<int, int>(distanceKm, durationMinutes));
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network or API errors
                _logger.LogError(ex, "[CalculateRoute] Error getting route");
                return ServiceResult<Tuple<int, int>>.Failure("Nepodařilo se vypočítat cestu!"); 
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors
                _logger.LogError(ex, "[CalculateRoute] Error getting route(parsing)");
                return ServiceResult<Tuple<int, int>>.Failure("Nepodařilo se vypočítat cestu (problémy s parsováním)!");
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                _logger.LogError(ex, "[CalculateRoute] Error getting route (unknown error)");
                return ServiceResult<Tuple<int, int>>.Failure("Nepodařilo se vypočítat cestu (neznáma chyba)!");
            }
        }
    }
}
