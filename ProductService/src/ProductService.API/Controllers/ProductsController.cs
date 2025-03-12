using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Синхронизация статуса пользователя (внутренний вызов)
    /// </summary>
    [HttpPost("sync-user/{userId}")]
    [AllowAnonymous]
    [ApiKeyAuth] // Кастомный атрибут для проверки API-ключа
    public async Task<IActionResult> SyncUserStatus(Guid userId, [FromBody] UserSyncDto dto)
    {
        await _productService.SyncUserStatusAsync(userId, dto.IsActive);
        return NoContent();
    }

    /// <summary>
    /// Создание нового продукта
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductDto dto)
    {
        var userId = GetCurrentUserId();
        var product = await _productService.CreateProductAsync(dto, userId);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>
    /// Получение продукта по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return Ok(product);
    }

    /// <summary>
    /// Получение продуктов с фильтрацией
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDto filter)
    {
        var products = await _productService.GetProductsAsync(filter);
        return Ok(products);
    }

    /// <summary>
    /// Обновление продукта
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductDto dto)
    {
        var userId = GetCurrentUserId();
        await _productService.UpdateProductAsync(id, dto, userId);
        return NoContent();
    }

    /// <summary>
    /// Мягкое удаление продукта
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var userId = GetCurrentUserId();
        await _productService.SoftDeleteProductAsync(id, userId);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value 
            ?? throw new UnauthorizedAccessException("Invalid JWT token");
        return Guid.Parse(claim);
    }
}

// Внесите UserSyncDto в папку Application/DTOs
public class UserSyncDto
{
    public bool IsActive { get; set; }
}