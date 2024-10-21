namespace Shared.Events;

public class StockNotReservedEvent
{
    public Guid OrderId { get; set; }
    public Guid BuyerId { get; set; }
}