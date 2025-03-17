using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using ProductService.Application.DTOs;
using Microsoft.Extensions.Configuration;

namespace ProductService.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IValidator<ProductDto> _validator;
    private readonly ILogger<ProductService> _logger;
    private readonly string _userServiceBaseUrl;

    public ProductService(
        IProductRepository repository,
        IHttpClientFactory httpClientFactory,
        IValidator<ProductDto> validator,
        ILogger<ProductService> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _validator = validator;
        _logger = logger;
        _userServiceBaseUrl = configuration["UserService:BaseUrl"] 
            ?? throw new ArgumentNullException("UserService:BaseUrl");
    }

    public async Task<Product> CreateProductAsync(ProductDto dto, Guid userId, string authorizationToken)
    {
        // Проверка активности пользователя
         await ValidateUserStatusAsync(userId, authorizationToken);

        // Валидация DTO
        await _validator.ValidateAndThrowAsync(dto);

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsAvailable = dto.IsAvailable,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _logger.LogInformation("Creating product {ProductName}", product.Name);
        return await _repository.AddAsync(product);
    }

	public async Task SyncUserStatusAsync(Guid userId, bool isActive, string authorizationToken)
{
    var client = _httpClientFactory.CreateClient("UserService");
    
    // Создаем запрос
    var request = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}/status")
    {
        Content = new StringContent(
            JsonSerializer.Serialize(new { isActive }),
            Encoding.UTF8,
            "application/json")
    };

    // Обрабатываем токен
    var token = authorizationToken.StartsWith("Bearer ") 
        ? authorizationToken.Substring(7) 
        : authorizationToken;
    
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    try
    {
        // Отправляем запрос
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to sync user status. Response: {StatusCode}, {Content}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to update user status: {response.StatusCode}");
        }

        _logger.LogInformation("User status updated successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error syncing user status");
        throw;
    }
}

    public async Task<Product> GetProductByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product ?? throw new KeyNotFoundException($"Product {id} not found");
    }
	
    public async Task<IEnumerable<Product>> GetProductsAsync(ProductFilterDto filter)
    {
        return await _repository.GetFilteredProductsAsync(
            filter.SearchTerm,
            filter.MinPrice,
            filter.MaxPrice,
            filter.IsAvailable,
            filter.UserId
        );
    }

    public async Task UpdateProductAsync(Guid id, ProductDto dto, Guid userId)
    {
        var product = await GetProductByIdAsync(id);

        if (product.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update product {ProductId}", userId, id);
            throw new UnauthorizedAccessException("Access denied");
        }

        product.Name = dto.Name ?? product.Name;
        product.Description = dto.Description ?? product.Description;
        product.Price = dto.Price;
        product.IsAvailable = dto.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(product);
        _logger.LogInformation("Product {ProductId} updated", id);
    }

    public async Task SoftDeleteProductAsync(Guid id, Guid userId)
    {
        var product = await GetProductByIdAsync(id);

        if (product.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete product {ProductId}", userId, id);
            throw new UnauthorizedAccessException("Access denied");
        }

        await _repository.SoftDeleteAsync(id);
        _logger.LogInformation("Product {ProductId} soft-deleted", id);
    }

    public async Task ValidateUserStatusAsync(Guid userId, string authorizationToken)
{
    var baseUri = new Uri(_userServiceBaseUrl);
    var userInfoUri = new Uri(baseUri, $"/api/users/{userId}");

	var client = _httpClientFactory.CreateClient();
    
    var request = new HttpRequestMessage(HttpMethod.Get, userInfoUri);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorizationToken);

    var response = await client.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException("Failed to fetch user status.");
    }

    var user = await response.Content.ReadFromJsonAsync<UserDto>();
    if (user == null || !user.IsActive)
    {
        throw new InvalidOperationException("User account is deactivated");
    }
}
}