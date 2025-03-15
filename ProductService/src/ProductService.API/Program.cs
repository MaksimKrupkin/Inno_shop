using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using FluentValidation.AspNetCore;
using ProductService.Infrastructure.Data;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Repositories;
using ProductService.Application.Interfaces;
using ProductService.Application.Validators;
using Microsoft.OpenApi.Models;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Загрузка переменных окружения
DotNetEnv.Env.Load();

// 1. Конфигурация HttpClient
builder.Services.AddHttpClient("UserService", client => 
{
    client.BaseAddress = new Uri(builder.Configuration["UserService:BaseUrl"]!);
});

builder.Services.AddControllers();

// 2. Аутентификация JWT (исправленная версия)
builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("Bearer", options =>
{
    // Настройки из конфигурации
    options.Authority = builder.Configuration["Jwt:Authority"];
    options.Audience = "product-service";
    
    // Дополнительные параметры валидации
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "yourIssuer",
        ValidAudience = "yourAudience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("yourSecretKey"))
    };
});

// 3. Конфигурация БД
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new Exception("Connection string 'DefaultConnection' not found in configuration");

builder.Services.AddDbContext<ProductDbContext>(options => 
    options.UseNpgsql(connectionString));

// 4. Регистрация сервисов
builder.Services.AddAuthorization();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService.Application.Services.ProductService>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<ProductDtoValidator>();

// 5. Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Product Service API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
});

var app = builder.Build();

// 6. Обработка исключений
app.UseExceptionHandler(exceptionHandlerApp => 
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        
        await context.Response.WriteAsJsonAsync(new ProblemDetails 
        {
            Title = exception?.GetType().Name,
            Detail = exception?.Message,
            Status = context.Response.StatusCode
        });
    });
});

// 7. Применение миграций БД
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        db.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error applying migrations: {ex.Message}");
    throw;
}

// 8. Настройка Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service V1");
    c.RoutePrefix = "swagger"; // Измените с "api/docs" на "swagger"
});

// 9. Middleware pipeline (исправленная версия)
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine($"Starting in {app.Environment.EnvironmentName} mode");
Console.WriteLine($"DB Connection: {connectionString}");

app.Run();