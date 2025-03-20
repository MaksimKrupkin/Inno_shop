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
using System.Net.Http.Headers;
using MassTransit; 
using Shared.Infrastructure.Events;
using ProductService.Application.Consumers;
using Shared.Infrastructure.MessageBrokers;

var builder = WebApplication.CreateBuilder(args);

// Загрузка переменных окружения
Env.Load();

// Добавление контроллеров (исправление ошибки AddControllers)
builder.Services.AddControllers(); // <-- Добавлено!

// 1. Конфигурация HttpClient с повторами и таймаутом
builder.Services.AddHttpClient("UserService", client => 
{
     client.BaseAddress = new Uri(builder.Configuration["UserService:BaseUrl"]); // Укажите правильный порт
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json")
    );
})
.ConfigurePrimaryHttpMessageHandler(() => 
    new HttpClientHandler {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => 
            builder.Environment.IsDevelopment()
    });

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrEmpty(jwtSecretKey))
{
    throw new InvalidOperationException("JWT Secret Key is not configured.");
}

// 2. Аутентификация JWT (исправлена синтаксическая ошибка)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["UserService:BaseUrl"]!);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductDeletionConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        
        

    });
});

// 3. Конфигурация БД
builder.Services.AddDbContext<ProductDbContext>(options => 
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)));

// 4. Регистрация сервисов с валидацией
builder.Services.AddSingleton<IMessageBroker, RabbitMqBroker>();
builder.Services.AddScoped<ProductDeletionConsumer>();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService.Application.Services.ProductService>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<ProductDtoValidator>();
builder.Services.AddMassTransitHostedService();

// 5. Настройка Swagger с Bearer Auth
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "Product Service API", 
        Version = "v1",
        Contact = new OpenApiContact {
            Name = "Support",
            Email = "support@productservice.com"
        }
    });

    var securityScheme = new OpenApiSecurityScheme {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { securityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddHttpClient("UserService", client => 
{
    client.BaseAddress = new Uri("http://localhost:5000"); // Порт UserService
});

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddLogging(loggingBuilder => 
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

var app = builder.Build();

// 6. Глобальная обработка ошибок
app.UseExceptionHandler(new ExceptionHandlerOptions {
    AllowStatusCode404Response = true,
    ExceptionHandler = async context => {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var problemDetails = new ProblemDetails {
            Title = exception?.GetType().Name,
            Detail = exception?.Message,
            Status = context.Response.StatusCode = exception switch {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ValidationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            }
        };

        if (app.Environment.IsDevelopment()) {
            problemDetails.Extensions["trace"] = exception?.StackTrace;
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
});

// 7. Автомиграции с продвинутым логированием
if (app.Environment.IsDevelopment()) {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    try {
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any()) {
            Console.WriteLine($"Applying migrations: {string.Join(", ", pendingMigrations)}");
            await db.Database.MigrateAsync();
            Console.WriteLine("Migrations applied successfully");
        }
    }
    catch (Exception ex) {
        Console.WriteLine($"Migration failed: {ex.Message}");
        throw;
    }
}

// 8. Конфигурация Swagger только для разработки
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service V1");
        c.OAuthClientId("swagger-ui");
        c.OAuthAppName("Swagger UI");
        c.RoutePrefix = "swagger";
    });
}

// 9. Middleware pipeline
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

// Health check без авторизации
app.MapGet("/health", () => Results.Ok(new {
    status = "OK",
    timestamp = DateTime.UtcNow
}));

// Логирование конфигурации
Console.WriteLine($"Application started in {app.Environment.EnvironmentName} mode");

// Правильное получение сервиса через scope
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetService<ProductDbContext>();
    Console.WriteLine($"Database provider: {dbContext?.Database.ProviderName}");
}

app.Run();