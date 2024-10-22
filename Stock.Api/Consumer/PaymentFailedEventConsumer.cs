using MassTransit;
using MongoDB.Driver;
using Shared.Events;
using Stock.Api.Services;

namespace Stock.Api.Consumer;

public class PaymentFailedEventConsumer(MongoDbService mongoDbService) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        IMongoCollection<Models.Stock> stockCollection = mongoDbService.GetCollection<Models.Stock>();

        foreach (var orderItem in context.Message.OrderItems)
        {
            Models.Stock stockInWarehouse =
                await (await stockCollection.FindAsync(q => q.ProductId == orderItem.ProductId.ToString())).FirstOrDefaultAsync();

            if (stockInWarehouse != null)
            {
                stockInWarehouse.Count += orderItem.Count;

                await stockCollection.FindOneAndReplaceAsync(c => c.ProductId == orderItem.ProductId.ToString(), stockInWarehouse);
            }
        }
    }
}