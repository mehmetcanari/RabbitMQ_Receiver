using OrderConsumer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RabbitMqConsumerService>();
builder.Services.AddHostedService<OrderConsumerBackgroundService>();

builder.Services.AddLogging();

var app = builder.Build();

app.MapGet("/", () => "Order Consumer Service is running!");

app.Run(); 