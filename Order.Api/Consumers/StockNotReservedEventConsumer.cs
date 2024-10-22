using MassTransit;
using Order.Api.Context;
using Order.Api.Enums;
using Shared.Events;

namespace Order.Api.Consumers;

public class StockNotReservedEventConsumer(OrderDbContext dbContext) : IConsumer<StockNotReservedEvent>
{
    public async Task Consume(ConsumeContext<StockNotReservedEvent> context)
    {
        var order = await dbContext.Orders.FindAsync(context.Message.OrderId);

        ArgumentNullException.ThrowIfNull(order);

        order.OrderStatus = OrderStatus.Fail;

        await dbContext.SaveChangesAsync();
    }
}