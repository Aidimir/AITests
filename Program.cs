using System.IdentityModel.Tokens.Jwt;
using AITests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient(); // Регистрируем IHttpClientFactory
builder.Services.AddSingleton<JwtSecurityTokenHandler, CustomTokenHandler>(); // Регистрируем наш кастомный TokenHandler

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables();

builder.Services.AddSwaggerGen(setup =>
{
    // Include 'SecurityScheme' to use JWT Authentication
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {jwtSecurityScheme, Array.Empty<string>()}
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var serviceProvider = builder.Services.BuildServiceProvider();
        options.TokenHandlers.Clear();
        options.TokenHandlers.Add(serviceProvider.GetRequiredService<JwtSecurityTokenHandler>());
    });
builder.Services.AddScoped<GlobalExceptionHandlerMiddleware>();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

// Add services to the container.

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddControllers();

// builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors(builder => builder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin()
);

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();