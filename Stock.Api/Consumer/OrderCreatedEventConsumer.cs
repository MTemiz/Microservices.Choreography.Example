using MassTransit;
using MongoDB.Driver;
using Shared;
using Shared.Events;
using Stock.Api.Services;

namespace Stock.Api.Consumer;

public class OrderCreatedEventConsumer(MongoDbService mongoDbService, IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider)
    : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        List<bool> stockResults = new();

        IMongoCollection<Models.Stock> stockCollection = mongoDbService.GetCollection<Models.Stock>();

        foreach (var orderItem in context.Message.OrderItems)
        {
            IAsyncCursor<Models.Stock> stocksInWarehouse =
                await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId.ToString() && s.Count >= orderItem.Count);

            stockResults.Add(await stocksInWarehouse.AnyAsync());
        }

        if (stockResults.TrueForAll(c => c.Equals(true)))
        {
            foreach (var orderItem in context.Message.OrderItems)
            {
                IAsyncCursor<Models.Stock> stocksInWarehouse =
                    await stockCollection.FindAsync(s => s.ProductId == orderItem.ProductId.ToString());

                var stockInWarehouse = await stocksInWarehouse.FirstOrDefaultAsync();

                stockInWarehouse.Count -= orderItem.Count;

                await stockCollection.FindOneAndReplaceAsync(c => c.ProductId == orderItem.ProductId.ToString(), stockInWarehouse);
            }

            StockReservedEvent stockReservedEvent = new()
            {
                BuyerId = context.Message.BuyerId,
                OrderId = context.Message.OrderId,
                OrderItems = context.Message.OrderItems,
                TotalPrice = context.Message.TotalPrice
            };

            ISendEndpoint sendEndpoint =
                await sendEndpointProvider.GetSendEndpoint(
                    new Uri($"queue:{RabbitMQSettings.Payment_StockReservedEventQueue}"));

            sendEndpoint.Send(stockReservedEvent);
        }
        else
        {
            StockNotReservedEvent stockNotReservedEvent = new()
            {
                BuyerId = context.Message.BuyerId,
                OrderId = context.Message.OrderId,
            };

            publishEndpoint.Publish(stockNotReservedEvent);
        }
    }
}