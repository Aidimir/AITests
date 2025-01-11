using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AITests;

public class CustomTokenHandler : JwtSecurityTokenHandler
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CustomTokenHandler> _logger;

    public CustomTokenHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<CustomTokenHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters,
        out SecurityToken validatedToken)
    {
        var jwtToken = ReadJwtToken(token);

        var httpClient = _httpClientFactory.CreateClient();

        var authorizationUrl = _configuration["Authorization:URL"];
        var response = httpClient.PostAsync(
            authorizationUrl,
            new StringContent($"\"{token}\"", Encoding.UTF8, "application/json")
        ).Result;

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = response.Content.ReadAsStringAsync().Result;
            _logger.LogInformation($"Sending request to {authorizationUrl}");
            _logger.LogError($"Token validation failed via external service. \n {responseContent}");
            throw new SecurityTokenException($"Token validation failed via external service. \n {responseContent}");
        }
        
        _logger.LogInformation("Token validated successfully");
        
        var identity = new ClaimsIdentity(jwtToken.Claims, "JWT");
        validatedToken = jwtToken;
        return new ClaimsPrincipal(identity);
    }
}