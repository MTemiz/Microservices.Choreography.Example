using MassTransit;
using Order.Api.Context;
using Order.Api.Enums;
using Shared.Events;

namespace Order.Api.Consumers;

public class PaymentFailedEventConsumer(OrderDbContext dbContext) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var order = await dbContext.Orders.FindAsync(context.Message.OrderId);
        
        ArgumentNullException.ThrowIfNull(order);
        
        order.OrderStatus = OrderStatus.Fail;

        await dbContext.SaveChangesAsync();
    }
}