namespace ProductService.Application.DTOs;

public class ProductDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}