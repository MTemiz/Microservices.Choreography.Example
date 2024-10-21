using MassTransit;
using Shared.Events;

namespace Payment.Api.Consumers;

public class StockReservedEventConsumer(IPublishEndpoint publishEndpoint) : IConsumer<StockReservedEvent>
{
    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        bool isSuccess = true;

        if (isSuccess)
        {
            PaymentCompletedEvent paymentCompletedEvent = new()
            {
                OrderId = context.Message.OrderId
            };

            await publishEndpoint.Publish(paymentCompletedEvent);

            await Console.Out.WriteLineAsync("Ödeme başarılı.");
        }
        else
        {
            PaymentFailedEvent paymentFailedEvent = new()
            {
                OrderId = context.Message.OrderId,
                OrderItems = context.Message.OrderItems
            };
            
            await publishEndpoint.Publish(paymentFailedEvent);
            
            await Console.Out.WriteLineAsync("Ödeme başarısız.");
        }
    }
}