// Add this controller to both your Admin and Referee services
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

namespace YourService.Controllers
{
    /// <summary>
    /// Controller for validating authentication tokens used by internal services.
    /// </summary>
    [Route("internal")]
    [ApiController]
    public class TokenValidationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
	private readonly ILogger<TokenValidationController> _logger;

        public TokenValidationController(IConfiguration configuration, ILogger<TokenValidationController> logger)
        {
	    _logger = logger;
            _configuration = configuration;
        }

	/// <summary>
        /// Validates the provided authentication token.
        /// </summary>
        /// <remarks>
        /// This endpoint expects the authentication token to be provided via a cookie named "auth_token".
        /// The token is validated using a secret key from AzureKeyVault, issuer, and audience values from Docker secrets.
        /// </remarks>
        /// <returns>200 OK if the token is valid, 401 Unauthorized if invalid, or 500 Internal Server Error if an exception occurs.</returns>
        /// <response code="200">Token is valid and user is authenticated</response>
        /// <response code="401">Invalid token or missing token</response>
        /// <response code="500">Error occurred during token validation</response>
        [HttpGet("validate-token")]
        public IActionResult ValidateToken()
        {
            try
            {
                string token = null;
                
                // Try to get from cookie first
                if (Request.Cookies.TryGetValue("auth_token", out string cookieToken))
                {
                    token = cookieToken;
                }

                if (string.IsNullOrEmpty(token))
                {
		    _logger.LogWarning("Token not provided in the request.");
                    return Unauthorized("");
                }

                // Validate the token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["SecretKey"]);
               

		// Read the secrets from the Docker secrets directory
	        var jwtIssuer = System.IO.File.ReadAllText("/run/secrets/JwtIssuer").Trim();
        	var jwtAudience = System.IO.File.ReadAllText("/run/secrets/JwtAudience").Trim();
            

                // Configure validation parameters
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Validate token
                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out _);

                var roleClaim = principal.FindFirst(ClaimTypes.Role);
                if (roleClaim == null)
                {
			_logger.LogWarning("Missing role claim in the token.");
                	return Forbid("Chybějící role, kontaktujte administrátora!");
                }

                if (roleClaim.Value != "Referee")
                {
                        _logger.LogWarning("Different role as expected.");
			return Forbid("Nedostatečné oprávnení!");
                }

		
		// After validation succeeds:
        	var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        	var username = principal.FindFirst(ClaimTypes.Name)?.Value;
        	var role = principal.FindFirst(ClaimTypes.Role)?.Value;
       
	         	
        	// Set user info in response headers
        	Response.Headers.Add("X-User-Id", userId);
        	Response.Headers.Add("X-User-Name", username);
        	Response.Headers.Add("X-User-Role", role);


		// Log the successful validation
                _logger.LogInformation("Token validated successfully for user {Username} with role {Role}.", username, role);
                return Ok();
            }
            catch (SecurityTokenExpiredException)
            {
		_logger.LogWarning("Token expired.");
            	return Unauthorized("");
            }
            catch (Exception ex)
            {
		 _logger.LogError(ex, "Error occurred during token validation.");
                 return StatusCode(500, "Token validation error");
            }
        }
	
	private string GetDockerSecret(string secretName)
        {
                string path = $"/run/secrets/{secretName}";
                return System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path).Trim() : null;
        }
    }
}
