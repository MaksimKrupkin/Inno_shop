namespace ProductService.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    
    public bool IsActive { get; set; } = true;
    public Guid UserId { get; set; } // ID создателя продукта
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false; // Soft Delete
}