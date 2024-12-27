using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AITests;

public class CustomTokenHandler : JwtSecurityTokenHandler
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public CustomTokenHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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
            throw new SecurityTokenException($"Token validation failed via external service. \n {responseContent}");
        }


        var identity = new ClaimsIdentity(jwtToken.Claims, "JWT");
        validatedToken = jwtToken;
        return new ClaimsPrincipal(identity);
    }
}