using ProductService.Domain.Entities;
using ProductService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Application.Interfaces
{
    public interface IProductService
    {
        Task<Product> CreateProductAsync(ProductDto dto, Guid userId, string authorizationToken);
        Task SyncUserStatusAsync(Guid userId, bool isActive, string authorizationToken);
        Task<Product> GetProductByIdAsync(Guid id);
        Task<IEnumerable<Product>> GetProductsAsync(ProductFilterDto filter);
        Task UpdateProductAsync(Guid id, ProductDto dto, Guid userId);
        Task SoftDeleteProductAsync(Guid productId, Guid userId);
        Task SoftDeleteAllUserProductsAsync(Guid userId);
        Task ValidateUserStatusAsync(Guid userId, string authorizationToken);
    }
}