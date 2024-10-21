using MassTransit;
using MassTransit.Transports;
using Microsoft.EntityFrameworkCore;
using Order.Api.Consumers;
using Order.Api.Context;
using Order.Api.Enums;
using Order.Api.ViewModels;
using Shared;
using Shared.Events;
using Shared.Messages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<PaymentCompletedEventConsumer>();

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration["RabbitMQ"]);

        _configure.ReceiveEndpoint(RabbitMQSettings.Order_PaymentCompletedEventQueue,
            e => e.ConfigureConsumer<PaymentCompletedEventConsumer>(context));
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/create-order", async (IPublishEndpoint publishEndpoint, CreateOrderVM model, OrderDbContext dbContext) =>
{
    Order.Api.Models.Order order = new Order.Api.Models.Order
    {
        BuyerId = Guid.TryParse(model.BuyerId, out Guid _buyerId) ? _buyerId : Guid.NewGuid(),
        OrderItems = model.OrderItems.Select(oi => new Order.Api.Models.OrderItem()
        {
            Count = oi.Count,
            Price = oi.Price,
            ProductId = Guid.Parse(oi.ProductId)
        }).ToList(),
        OrderStatus = OrderStatus.Suspend,
        CreatedDate = DateTime.Now,
        TotalPrice = model.OrderItems.Sum(oi => oi.Count * oi.Price),
    };

    dbContext.Orders.Add(order);

    await dbContext.SaveChangesAsync();

    OrderCreatedEvent orderCreatedEvent = new()
    {
        BuyerId = order.BuyerId,
        OrderId = order.Id,
        TotalPrice = order.TotalPrice,
        OrderItems = order.OrderItems.Select(oi => new OrderItemMessage()
        {
            ProductId = oi.ProductId,
            Count = oi.Count,
            Price = oi.Price
        }).ToList()
    };

    await publishEndpoint.Publish(orderCreatedEvent);
});

app.Run();