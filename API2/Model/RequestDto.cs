namespace API2.Model;

public class RequestDto
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}