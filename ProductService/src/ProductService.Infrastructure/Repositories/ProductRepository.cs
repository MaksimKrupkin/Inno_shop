using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Product>> GetFilteredProductsAsync(
        string? searchTerm,
        decimal? minPrice,
        decimal? maxPrice,
        bool? isAvailable,
        Guid? userId)
    {
        var query = _context.Products.AsQueryable();

        // Базовый фильтр по soft delete
        query = query.Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => 
                p.Name.Contains(searchTerm) || 
                (p.Description != null && p.Description.Contains(searchTerm)));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        if (isAvailable.HasValue)
            query = query.Where(p => p.IsAvailable == isAvailable.Value);

        if (userId.HasValue)
            query = query.Where(p => p.UserId == userId.Value);

        return await query.ToListAsync();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var product = await GetByIdAsync(id);
        if (product != null)
        {
            product.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RestoreAsync(Guid id)
    {
        var product = await GetByIdAsync(id);
        if (product != null)
        {
            product.IsDeleted = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SoftDeleteProductsByUserIdAsync(Guid userId)
    {
        var products = await _context.Products
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .ToListAsync();

        foreach (var product in products)
        {
            product.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RestoreProductsByUserIdAsync(Guid userId)
    {
        var products = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId && p.IsDeleted)
            .ToListAsync();

        foreach (var product in products)
        {
            product.IsDeleted = false;
        }

        await _context.SaveChangesAsync();
    }
}