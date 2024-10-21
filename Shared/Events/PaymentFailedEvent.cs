using Shared.Messages;

namespace Shared.Events;

public class PaymentFailedEvent
{
    public Guid OrderId { get; set; }
    public List<OrderItemMessage> OrderItems { get; set; }
}