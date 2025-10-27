using AdminPartDevelop.Common;
using System.Globalization;
using System.Text.Json;
using System.Text;

namespace AdminPartDevelop.Services.EmailsSender
{
    public class EmailsToLoginDbSender
    {
        private readonly ILogger<EmailsToLoginDbSender> _logger;
	private readonly HttpClient _httpClient;
        
	public EmailsToLoginDbSender(
        	HttpClient httpClient,
		ILogger<EmailsToLoginDbSender> logger
            )
        {
	    _httpClient = httpClient;
            _logger = logger;
        }


	public async Task<ServiceResult<bool>> AddEmailsToAllowedListAsync(List<string> emails)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(emails);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

		 var response = await _httpClient.PostAsync(
                    "http://172.18.3.2:8080/internal/user-emails",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully added emails to allowed list");
                    return ServiceResult<bool>.Success(true);
		}
                else
                {
                    _logger.LogWarning($"Failed to add email to allowed list. Status: {response.StatusCode}");
                    return ServiceResult<bool>.Success(false);
		}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding emails to allowed list");
            	return ServiceResult<bool>.Failure("Nepodařilo se uložit emaily do databáze!");
	    }
        }

    }
}
