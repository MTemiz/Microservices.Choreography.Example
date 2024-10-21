using MassTransit;
using Order.Api.Context;
using Order.Api.Enums;
using Shared.Events;

namespace Order.Api.Consumers;

public class PaymentCompletedEventConsumer(OrderDbContext dbContext) : IConsumer<PaymentCompletedEvent>
{
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var order = await dbContext.Orders.FindAsync(context.Message.OrderId);
        
        ArgumentNullException.ThrowIfNull(order);
        
        order.OrderStatus = OrderStatus.Completed;
        
        await dbContext.SaveChangesAsync();
    }
}