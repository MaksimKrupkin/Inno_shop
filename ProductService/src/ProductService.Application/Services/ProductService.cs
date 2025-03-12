using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ProductService.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IValidator<ProductDto> _validator;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository repository,
        IHttpClientFactory httpClientFactory,
        IValidator<ProductDto> validator,
        ILogger<ProductService> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Product> CreateProductAsync(ProductDto dto, Guid userId)
    {
        // Проверка активности пользователя
        await ValidateUserStatusAsync(userId);
        
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

    public async Task<Product> GetProductByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product ?? throw new KeyNotFoundException($"Product {id} not found");
    }

    public async Task<IEnumerable<Product>> GetProductsAsync(ProductFilterDto filter)
    {
        _logger.LogDebug("Fetching products with filter: {@Filter}", filter);
        return await _repository.GetFilteredProductsAsync(
            filter.SearchTerm,
            filter.MinPrice,
            filter.MaxPrice,
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

    public async Task SyncUserStatusAsync(Guid userId, bool isActive)
    {
        _logger.LogInformation("Syncing user status: User {UserId}, Active: {IsActive}", userId, isActive);
        
        if (!isActive)
            await _repository.SoftDeleteProductsByUserIdAsync(userId);
        else
            await _repository.RestoreProductsByUserIdAsync(userId);
    }

    private async Task ValidateUserStatusAsync(Guid userId)
    {
        var client = _httpClientFactory.CreateClient("UserService");
        var response = await client.GetAsync($"/api/users/{userId}/status");
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("User {UserId} is inactive", userId);
            throw new InvalidOperationException("User account is deactivated");
        }
    }
}