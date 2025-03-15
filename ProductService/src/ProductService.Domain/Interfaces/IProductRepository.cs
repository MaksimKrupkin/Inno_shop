using ProductService.Domain.Entities;


namespace ProductService.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task<IEnumerable<Product>> GetFilteredProductsAsync(
        string? searchTerm,
        decimal? minPrice,
        decimal? maxPrice,
        bool? isAvailable,
        Guid? userId);
    Task SoftDeleteAsync(Guid id);
    Task RestoreAsync(Guid id);
    Task SoftDeleteProductsByUserIdAsync(Guid userId);
    Task RestoreProductsByUserIdAsync(Guid userId);
}
