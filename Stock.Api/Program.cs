using MassTransit;
using MongoDB.Driver;
using Shared;
using Stock.Api.Consumer;
using Stock.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoDbService>();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCreatedEventConsumer>();
    configurator.AddConsumer<PaymentFailedEventConsumer>();

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration["RabbitMQ"]);

        _configure.ReceiveEndpoint(RabbitMQSettings.Stock_OrderCreatedEventQueue,
            e => e.ConfigureConsumer<OrderCreatedEventConsumer>(context));

        _configure.ReceiveEndpoint(RabbitMQSettings.Stock_PaymentFailedEventQueue,
            e => e.ConfigureConsumer<PaymentFailedEventConsumer>(context));
    });
});

var app = builder.Build();

using IServiceScope serviceScope = app.Services.CreateScope();

var mongoDbService = serviceScope.ServiceProvider.GetRequiredService<MongoDbService>();

IMongoCollection<Stock.Api.Models.Stock> stockCollection = mongoDbService.GetCollection<Stock.Api.Models.Stock>();

var stocksInWarehouse = await stockCollection.FindAsync(session => true);

if (!stocksInWarehouse.Any())
{
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = Guid.NewGuid().ToString(), Count = 20 });
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = Guid.NewGuid().ToString(), Count = 50 });
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = Guid.NewGuid().ToString(), Count = 100 });
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = Guid.NewGuid().ToString(), Count = 200 });
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = Guid.NewGuid().ToString(), Count = 300 });
    await stockCollection.InsertOneAsync(new Stock.Api.Models.Stock() { ProductId = Guid.NewGuid().ToString(), Count = 400 });
}

app.Run();