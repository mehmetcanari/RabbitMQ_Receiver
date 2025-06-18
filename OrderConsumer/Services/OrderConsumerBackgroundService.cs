using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderConsumer.Models;

namespace OrderConsumer.Services;

public class OrderConsumerBackgroundService : BackgroundService
{
    private readonly RabbitMqConsumerService _rabbitMQService;
    private readonly ILogger<OrderConsumerBackgroundService> _logger;

    public OrderConsumerBackgroundService(
        RabbitMqConsumerService rabbitMQService,
        ILogger<OrderConsumerBackgroundService> logger)
    {
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Order Consumer Background Service");
        
        try
        {
            _rabbitMQService.StartConsuming(HandleOrderCreatedEvent);
            _logger.LogInformation("Successfully started consuming messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming messages");
            throw;
        }
        
        return Task.CompletedTask;
    }

    private async Task HandleOrderCreatedEvent(OrderCreatedEvent orderEvent)
    {
        _logger.LogInformation("=== ORDER PROCESSING STARTED ===");
        _logger.LogInformation("Processing order created event for OrderId: {OrderId}", orderEvent.OrderId);
        _logger.LogInformation("Order details: AccountId={AccountId}, TotalPrice={TotalPrice}, Status={Status}", 
            orderEvent.AccountId, orderEvent.TotalPrice, orderEvent.Status);
        _logger.LogInformation("Shipping Address: {ShippingAddress}", orderEvent.ShippingAddress);
        _logger.LogInformation("Billing Address: {BillingAddress}", orderEvent.BillingAddress);
        _logger.LogInformation("Order Date: {OrderDate}", orderEvent.OrderDate);
        
        try
        {
            // Burada sipariş işleme mantığınızı yazabilirsiniz
            // Örneğin:
            // - Email gönderme
            // - SMS gönderme
            // - Stok güncelleme
            // - Fatura oluşturma
            // - Kargo firmasına bildirim gönderme
            
            await Task.Delay(1000); // Simüle edilmiş işlem süresi
            
            _logger.LogInformation("=== ORDER PROCESSING COMPLETED ===");
            _logger.LogInformation("Successfully processed order created event for OrderId: {OrderId}", orderEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order event for OrderId: {OrderId}", orderEvent.OrderId);
            throw;
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("Disposing OrderConsumerBackgroundService");
        _rabbitMQService?.Dispose();
        base.Dispose();
    }
} 