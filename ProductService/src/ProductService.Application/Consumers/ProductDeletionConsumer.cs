using MassTransit;
using Shared.Infrastructure.Events;
using ProductService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ProductService.Application.Consumers;

public class ProductDeletionConsumer : IConsumer<UserDeletedEvent>
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductDeletionConsumer> _logger;

    public ProductDeletionConsumer(
        IProductService productService,
        ILogger<ProductDeletionConsumer> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        try 
        {
            _logger.LogInformation("Processing user deletion for {UserId}", context.Message.UserId);
            await _productService.SoftDeleteAllUserProductsAsync(context.Message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process user deletion event");
        }
    }
}