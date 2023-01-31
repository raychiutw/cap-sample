namespace PMP.EdgeService.Domain.Entities;

public class Order
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Qty { get; set; }
}