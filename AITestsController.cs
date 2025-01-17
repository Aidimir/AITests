using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using AITests.Models.Response;
using Dal;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
public class AITestsController : ControllerBase
{
    private const string Instruction =
        "Ты должен выполнять анализировать технические задания, сделанные по единому образцу," +
        " анализировать их и переводить на понятный человеку простой язык. После этого ты должен" +
        "создавать по этому тз тест кейсы в таком формате: «Сценарий: Создание учетной записи, Шаги сценария: - Открыть экран регистрации. - Ввести корректный адрес электронной почты. - Ввести надежный пароль. - Подтвердить пароль. - Нажать кнопку 'Зарегистрироваться'., Ожидаемый результат: Пользователь успешно зарегистрирован, отображается сообщение о подтверждении регистрации.»";

    private readonly string _aiUrl;
    private readonly string _apiKey;

    private readonly ReqRespContext _reqRespContext;
    private readonly string _version;

    public AITestsController(IConfiguration configuration, ReqRespContext reqRespContext)
    {
        _aiUrl = configuration["AI:URL"];
        _apiKey = configuration["AI:key"];
        _version = configuration["AI:version"];
        _reqRespContext = reqRespContext;
    }

    private Guid UserId
    {
        get
        {
            if (!Guid.TryParse(User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)?.Value,
                    out var id))
                throw new AuthenticationException("No user id was provided");
            return id;
        }
    }

    [HttpPost("generateTZ")]
    public async Task<IActionResult> SendAiRequest(IFormFile technicalTask)
    {
        var xmlContent = ExtractFromDocx(technicalTask);

        var response = await SendRequestToChatGpt(xmlContent, Instruction);

        if (!response.httpResponseMessage.IsSuccessStatusCode)
            return new ContentResult
            {
                Content = response.httpResponseMessage.Content.ReadAsStringAsync().Result,
                StatusCode = (int) response.httpResponseMessage.StatusCode
            };
        
        return Ok(response.reqRespModel);
    }

    private async Task<(HttpResponseMessage httpResponseMessage, ReqRespModel? reqRespModel)> SendRequestToChatGpt(string fileContent, string instruction)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var requestBody = new
            {
                model = _version,
                messages = new[]
                {
                    new {role = "system", content = instruction},
                    new {role = "user", content = $"Файл в формате xml: {fileContent}"}
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response =
                await client.PostAsync($"{_aiUrl}/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var deserializedResponse = JsonSerializer.Deserialize<ChatResponse>(responseString);

                var responseContent = deserializedResponse.Choices.Last().Message.Content;

                var model = new ReqRespModel
                {
                    Id = Guid.NewGuid(),
                    UserId = UserId,
                    FileContent = fileContent,
                    ResponseContent = responseContent.Replace("\\n", Environment.NewLine)
                };

                await _reqRespContext.AddToDbAsync(model);
                return (response, model);
            }

            return (response, null);
        }
    }

    [HttpGet("tz-history")]
    public async Task<IActionResult> GetHistory()
    {
        return Ok(await _reqRespContext.GetAllUsersModelAsync(UserId));
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveHistory(Guid id)
    {
        await _reqRespContext.RemoveReqRespModelAsync(id, UserId);
        return NoContent();
    }

    private string? ExtractFromDocx(IFormFile file)
    {
        using var wordDoc = WordprocessingDocument.Open(file.OpenReadStream(), false);
        return wordDoc.MainDocumentPart?.Document.Body?.InnerText;
    }
}