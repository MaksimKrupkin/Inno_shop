using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.API.Filters;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
	private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger; // Инициализируем логгер
    }

    /// <summary>
    /// Синхронизация статуса пользователя (внутренний вызов)
    /// </summary>
    [HttpPost("sync-user/{userId}")]
public async Task<IActionResult> SyncUserStatus(
    Guid userId, 
    [FromBody] UserSyncDto dto,
    [FromHeader(Name = "Authorization")] string authorizationToken)
{
    await _productService.SyncUserStatusAsync(userId, dto.IsActive, authorizationToken);
    return NoContent();
}

    /// <summary>
    /// Создание нового продукта
    /// </summary>
    [HttpPost]
public async Task<IActionResult> CreateProduct(
    [FromBody] ProductDto dto,
    [FromHeader(Name = "Authorization")] string authToken)
{
    try
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Creating product for user {UserId}", userId);
        
        var product = await _productService.CreateProductAsync(dto, userId, authToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "User account deactivated during product creation");
        return StatusCode(500, new { error = ex.Message });
    }
}

    /// <summary>
    /// Получение продукта по ID
    /// </summary>
	[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(Guid id)
{
    var product = await _productService.GetProductByIdAsync(id);
    if (product == null)
    {
        return NotFound("Product not found");
    }
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
    // Используйте кастомный claim или стандартный "sub"
    var userIdClaim = User.Claims.FirstOrDefault(c => 
        c.Type == ClaimTypes.NameIdentifier || 
        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    );

    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
    {
        throw new UnauthorizedAccessException("Invalid JWT token");
    }
    return userId;
}
}

// Внесите UserSyncDto в папку Application/DTOs
public class UserSyncDto
{
    public bool IsActive { get; set; }
}