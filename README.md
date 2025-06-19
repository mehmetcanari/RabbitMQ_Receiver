# Order Consumer Microservice

This microservice listens to order-related messages from RabbitMQ queue published by the e-commerce system and processes them asynchronously. It consumes order requests from the main e-commerce application and handles order processing operations.

## Purpose

- Listen to order messages from RabbitMQ queue sent by e-commerce system
- Process order operations asynchronously

## Technology Stack

- **.NET 9.0** 
- **RabbitMQ.Client** 

## Project Structure

```
OrderConsumer/
├── Models/
│   └── [Order related models]
├── Services/
│   ├── OrderConsumerBackgroundService.cs
│   └── RabbitMQConsumerService.cs
├── OrderConsumer.csproj
├── Program.cs
├── appsettings.json
└── consumer.log
```
