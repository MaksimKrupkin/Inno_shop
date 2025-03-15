using ProductService.Application.DTOs;
using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces;

public interface IProductService
{
    Task<Product> CreateProductAsync(ProductDto dto, Guid userId);
    Task<Product> GetProductByIdAsync(Guid id);
    Task<IEnumerable<Product>> GetProductsAsync(ProductFilterDto filter);
    Task UpdateProductAsync(Guid id, ProductDto dto, Guid userId);
    Task SoftDeleteProductAsync(Guid id, Guid userId);
    Task SyncUserStatusAsync(Guid userId, bool isActive);
}