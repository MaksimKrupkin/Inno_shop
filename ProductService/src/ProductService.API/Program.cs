var builder = WebApplication.CreateBuilder(args);

// 1. Конфигурация HttpClient для UserService
builder.Services.AddHttpClient("UserService", client =>
{
    var baseUrl = builder.Configuration["UserService:BaseUrl"] 
        ?? throw new ArgumentNullException("UserService:BaseUrl is not configured");
    client.BaseAddress = new Uri(baseUrl);
});

// 2. JWT-аутентификация
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"] 
            ?? throw new ArgumentNullException("Jwt:Authority is not configured");
        options.Audience = "product-service";
    });

// 3. Entity Framework
builder.Services.AddDbContext<ProductDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new ArgumentNullException("Connection string is not configured");
    options.UseNpgsql(connectionString);
});

// 4. Регистрация сервисов
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddValidatorsFromAssemblyContaining<ProductDtoValidator>();

// 5. Добавьте Swagger для документации API (опционально)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 6. Настройка глобального обработчика ошибок
app.UseExceptionHandler(exceptionHandlerApp => 
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails 
        {
            Title = exception?.GetType().Name,
            Detail = exception?.Message,
            Status = statusCode
        });
        
        // Логирование ошибки
        app.Logger.LogError(exception, "Global exception handler");
    });
});

// 7. Автоматические миграции БД
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await dbContext.Database.MigrateAsync();
}

// 8. Swagger UI (только для разработки)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();