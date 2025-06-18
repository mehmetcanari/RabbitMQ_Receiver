using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderConsumer.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderConsumer.Services;

public class RabbitMqConsumerService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IConfiguration _configuration;

    public RabbitMqConsumerService(IConfiguration configuration, ILogger<RabbitMqConsumerService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"],
            UserName = configuration["RabbitMQ:UserName"],
            Password = configuration["RabbitMQ:Password"],
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672")
        };

        _logger.LogInformation("Connecting to RabbitMQ at {HostName}:{Port}", 
            factory.HostName, factory.Port);

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("Successfully connected to RabbitMQ");
            
            // Exchange'i tanımla
            _channel.ExchangeDeclare("order_exchange", ExchangeType.Direct, true, false, null);
            _logger.LogInformation("Exchange 'order_exchange' declared");
            
            // Queue'yu tanımla
            var queueResult = _channel.QueueDeclare("order_created_queue", true, false, false, null);
            _logger.LogInformation("Queue 'order_created_queue' declared with {MessageCount} messages", 
                queueResult.MessageCount);
            
            // Queue'yu exchange'e bağla
            _channel.QueueBind("order_created_queue", "order_exchange", "order.created");
            _logger.LogInformation("Queue 'order_created_queue' bound to exchange 'order_exchange' with routing key 'order.created'");
            
            // QoS ayarları
            _channel.BasicQos(0, 1, false);
            _logger.LogInformation("QoS settings applied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public void StartConsuming(Func<OrderCreatedEvent, Task> messageHandler)
    {
        _logger.LogInformation("Setting up consumer for queue: order_created_queue");
        
        // Connection ve channel durumunu kontrol et
        _logger.LogInformation("Connection is open: {IsOpen}", _connection.IsOpen);
        _logger.LogInformation("Channel is open: {IsOpen}", _channel.IsOpen);
        
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += (model, ea) =>
        {
            _logger.LogInformation("=== MESSAGE RECEIVED ===");
            _logger.LogInformation("Received message with delivery tag: {DeliveryTag}", ea.DeliveryTag);
            _logger.LogInformation("Message routing key: {RoutingKey}", ea.RoutingKey);
            _logger.LogInformation("Message exchange: {Exchange}", ea.Exchange);
            
            string messageJson = string.Empty;
            
            try
            {
                var body = ea.Body.ToArray();
                messageJson = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Raw message received: {Message}", messageJson);
                
                var message = JsonSerializer.Deserialize<OrderCreatedEvent>(messageJson);
                
                if (message != null)
                {
                    _logger.LogInformation("Successfully deserialized message: OrderId={OrderId}, AccountId={AccountId}, TotalPrice={TotalPrice}", 
                        message.OrderId, message.AccountId, message.TotalPrice);
                    
                    // Async handler'ı Task.Run ile çalıştır
                    Task.Run(async () =>
                    {
                        try
                        {
                            await messageHandler(message);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            _logger.LogInformation("Successfully processed and acknowledged message: OrderId={OrderId}", message.OrderId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in message handler for OrderId: {OrderId}", message.OrderId);
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize message to OrderCreatedEvent");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error for message: {RawMessage}", messageJson);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue: {RawMessage}", messageJson);
                _channel.BasicNack(ea.DeliveryTag, false, true); // Mesajı tekrar kuyruğa al
            }
        };

        consumer.Shutdown += (model, ea) =>
        {
            _logger.LogWarning("Consumer shutdown: {Reason}", ea.Initiator);
        };

        consumer.ConsumerCancelled += (model, ea) =>
        {
            _logger.LogWarning("Consumer cancelled");
        };

        var consumerTag = _channel.BasicConsume("order_created_queue", false, consumer);
        _logger.LogInformation("Started consuming messages from order_created_queue with consumer tag: {ConsumerTag}", consumerTag);
        
        var queueInfo = _channel.QueueDeclarePassive("order_created_queue");
        _logger.LogInformation("Queue status - Messages: {MessageCount}, Consumers: {ConsumerCount}", 
            queueInfo.MessageCount, queueInfo.ConsumerCount);
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing RabbitMQ connection");
        _channel?.Dispose();
        _connection?.Dispose();
    }
} 