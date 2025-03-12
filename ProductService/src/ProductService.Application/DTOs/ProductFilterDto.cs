namespace ProductService.Application.DTOs;

public class ProductFilterDto
{
    /// <summary> Поиск по названию или описанию </summary>
    public string? SearchTerm { get; set; }

    /// <summary> Минимальная цена </summary>
    public decimal? MinPrice { get; set; }

    /// <summary> Максимальная цена </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary> Фильтр по доступности </summary>
    public bool? IsAvailable { get; set; }

    /// <summary> Фильтр по ID владельца </summary>
    public Guid? UserId { get; set; }
}