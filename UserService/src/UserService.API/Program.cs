using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using UserService.Application.Services;
using UserService.Infrastructure.Data;
using UserService.Application.DTOs;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;
using System.Security.Claims; 
using UserService.API.Middleware;
using UserService.Application.Validators;
using UserService.Domain.Interfaces;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using BCrypt.Net;
using UserService.Application.Models;
using System.Text.Json; 
using UserService.API.Filters;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Configuration
    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "src", "UserService.API"))
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// Регистрация сервисов
builder.Services.AddControllers();

// База данных
builder.Services.AddDbContext<UserServiceDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserDb")));

// Валидация и сервисы
builder.Services.AddScoped<IValidator<UserDto>, UserDtoValidator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService.Application.Services.UserService>();
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000); // Слушать все IP-адреса на порту 5000
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)); 

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = secretKey,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            // Включите валидацию ролей
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

// Авторизация
builder.Services.AddAuthorization(options => 
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));

// Swagger
builder.Services.AddSwaggerGen(c => 
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "UserService API", 
        Version = "v1",
        Description = "User Management API"
    });
    
    // Настройка Bearer-авторизации
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme 
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в формате: Bearer <ваш_токен>"
    });
    
    // Глобальное требование авторизации
    c.AddSecurityRequirement(new OpenApiSecurityRequirement 
    {
        {
            new OpenApiSecurityScheme 
            { 
                Reference = new OpenApiReference 
                { 
                    Type = ReferenceType.SecurityScheme, 
                    Id = "Bearer" 
                } 
            },
            new List<string>() 
        }
    });
	c.OperationFilter<AuthResponsesOperationFilter>(); 
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    await next();
    
    if (context.Response.StatusCode == 403)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { Error = "Доступ запрещен" })
        );
    }
});

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserService v1");
    c.RoutePrefix = string.Empty;
});

// Миграции и создание тестового администратора
using (var scope = app.Services.CreateScope()) 
{
    var context = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
    
    // Применение миграций
    context.Database.Migrate();
    
    // Создание администратора, если его нет
    if (!context.Users.Any(u => u.Email == "admin@example.com"))
    {
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(admin);
        context.SaveChanges();
    }
}

app.MapControllers();
app.Run();