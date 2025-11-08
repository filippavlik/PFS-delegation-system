using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AdminPartDevelop.Common;
using Microsoft.Extensions.Logging;

namespace AdminPartDevelop.Services.RouteServices
{
    /// <summary>
    /// Implements the IRoutePlanner interface to calculate routes using public transportation
    /// via the Google Maps Directions API.
    /// </summary>
    public class RouteByBusPlanner : IRouteBusPlanner
    {
        private readonly ILogger<RouteByBusPlanner> _logger;        
        private readonly HttpClient _httpClient;                    // HTTP client for making API requests (managed by HttpClientFactory)
        private readonly string _apiKey;                            // API key for authenticating with Google Maps API
        private const string BaseUrl = "https://maps.googleapis.com/maps/api/directions/json";  // Base URL for the Google Directions API

        /// <summary>
        /// Constructor initializes the route planner with necessary dependencies using HttpClientFactory pattern
        /// </summary>
        /// <param name="logger">Logger for recording events and errors</param>
        /// <param name="httpClientFactory">Factory for creating managed HttpClient instances</param>
        /// <param name="apiKey">API key for Google Maps service</param>
        /// <exception cref="ArgumentNullException">Thrown when apiKey is null</exception>
        public RouteByBusPlanner(
            ILogger<RouteByBusPlanner> logger,
            IHttpClientFactory httpClientFactory,
            string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("GoogleMapsClient");  // Get a managed HttpClient instance
        }

        /// <summary>
        /// Calculates a route between two geographic coordinates using the Google Maps Directions API
        /// with public transportation options
        /// </summary>
        /// <param name="startLatitude">Starting point latitude</param>
        /// <param name="startLongitude">Starting point longitude</param>
        /// <param name="endLatitude">Destination latitude</param>
        /// <param name="endLongitude">Destination longitude</param>
        /// <param name="departureTime">Optional departure time for transit routing</param>
        /// <returns>
        /// A ServiceResult containing a Tuple with distance in kilometers and duration in minutes,
        /// or an error message if the calculation fails
        /// </returns>
        public async Task<ServiceResult<Tuple<int, int>>> CalculateRoute(float startLatitude, float startLongitude, float endLatitude, float endLongitude, DateTime? departureTime = null)
        {
            try
            {
                // Format coordinates with invariant culture to ensure decimal points
                string startLatStr = startLatitude.ToString(CultureInfo.InvariantCulture);
                string startLonStr = startLongitude.ToString(CultureInfo.InvariantCulture);
                string endLatStr = endLatitude.ToString(CultureInfo.InvariantCulture);
                string endLonStr = endLongitude.ToString(CultureInfo.InvariantCulture);

                string origin = $"{startLatStr},{startLonStr}";
                string destination = $"{endLatStr},{endLonStr}";

               
                var urlBuilder = $"{BaseUrl}?" +
                    $"origin={origin}" +
                    $"&destination={destination}" +
                    $"&mode=transit" +   
                    $"&language=cs";

                // Add departure_time if specified (Google requires Unix timestamp in seconds)
                if (departureTime.HasValue && departureTime.Value > DateTime.Now)
                {
                    var unixTime = ((DateTimeOffset)departureTime.Value).ToUnixTimeSeconds();
                    urlBuilder += $"&departure_time={unixTime}";
                }

                var url = urlBuilder + $"&key={_apiKey}";


                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();  
                var jsonResponse = await response.Content.ReadAsStringAsync();

                using (JsonDocument document = JsonDocument.Parse(jsonResponse))
                {
                    var root = document.RootElement;

                    string status = root.GetProperty("status").GetString();
                    if (status != "OK")
                    {
                        _logger.LogError($"[CalculateRoute] Google API returned status: {status}");
                        return ServiceResult<Tuple<int, int>>.Failure("Nepodařilo se vypočítat cestu!");  
                    }

                    var route = root.GetProperty("routes")[0];
                    var leg = route.GetProperty("legs")[0];  

                    int distanceMeters = leg.GetProperty("distance").GetProperty("value").GetInt32();
                    int durationSeconds = leg.GetProperty("duration").GetProperty("value").GetInt32();

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
