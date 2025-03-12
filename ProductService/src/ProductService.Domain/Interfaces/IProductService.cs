using ProductService.Application.DTOs;
using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces;

public interface IProductService
{
    // Основные методы
    Task<Product> CreateProductAsync(ProductDto dto, Guid userId);
    Task<Product> GetProductByIdAsync(Guid id);
    Task<IEnumerable<Product>> GetProductsAsync(ProductFilterDto filter);
    Task UpdateProductAsync(Guid id, ProductDto dto, Guid userId);
    Task SoftDeleteProductAsync(Guid id, Guid userId);
    
    // Метод синхронизации статуса пользователя
    Task SyncUserStatusAsync(Guid userId, bool isActive);
}