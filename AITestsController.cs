using System.Text;
using System.Text.Json;
using AITests.Models.Response;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class AITestsController : ControllerBase
{
    private const string Instruction =
        "Ты должен выполнять анализировать технические задания, сделанные по единому образцу," +
        " анализировать их и переводить на понятный человеку простой язык. После этого ты должен" +
        "создавать по этому тз тест кейсы в таком формате: «Сценарий: Создание учетной записи, Шаги сценария: - Открыть экран регистрации. - Ввести корректный адрес электронной почты. - Ввести надежный пароль. - Подтвердить пароль. - Нажать кнопку 'Зарегистрироваться'., Ожидаемый результат: Пользователь успешно зарегистрирован, отображается сообщение о подтверждении регистрации.»";

    private readonly string _aiUrl;
    private readonly string _apiKey;
    private readonly string _version;

    public AITestsController(IConfiguration configuration)
    {
        _aiUrl = configuration["AI:URL"];
        _apiKey = configuration["AI:key"];
        _version = configuration["AI:version"];
    }

    [HttpPost("generateTZ")]
    public async Task<IActionResult> SendAiRequest(IFormFile technicalTask)
    {
        var xmlContent = ExtractFromDocx(technicalTask);

        var json = await SendRequestToChatGpt(xmlContent, Instruction);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        var memoryStream = new MemoryStream();
        var textContent = response.Choices.Last().Message.Content;
        string formattedContent = textContent.Replace("\\n", Environment.NewLine);
        CreateWordDocument(memoryStream, textContent);
        memoryStream.Seek(0, SeekOrigin.Begin);
            
        return Ok(formattedContent);
    }

    private async Task<string> SendRequestToChatGpt(string fileContent, string instruction)
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
                return await response.Content.ReadAsStringAsync();

            return $"Ошибка: {response.StatusCode}";
        }
    }

    private string? ExtractFromDocx(IFormFile file)
    {
        using var wordDoc = WordprocessingDocument.Open(file.OpenReadStream(), false);
        return wordDoc.MainDocumentPart?.Document.Body?.InnerText;
    }

    private void CreateWordDocument(MemoryStream stream, string content)
    {
        var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        foreach (var line in content.Split('\n'))
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(line));
        }

        mainPart.Document.Save();
    }
}