namespace ProductService.Domain.Interfaces;

namespace ProductService.Domain.Interfaces;

public interface IProductRepository
{
    // Основные CRUD-методы
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    
    // Фильтрация и поиск
    Task<IEnumerable<Product>> GetFilteredProductsAsync(
        string? searchTerm,
        decimal? minPrice,
        decimal? maxPrice,
        bool? isAvailable,
        Guid? userId
    );
    
    // Мягкое удаление/восстановление
    Task SoftDeleteAsync(Guid id);
    Task RestoreAsync(Guid id);
    
    // Массовые операции для синхронизации с UserService
    Task SoftDeleteProductsByUserIdAsync(Guid userId);
    Task RestoreProductsByUserIdAsync(Guid userId);
}