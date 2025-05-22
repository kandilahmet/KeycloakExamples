namespace OrderService.Core.Domain.Entities
{
    public class Order:BaseEntity
    {
        public Guid BuyerId { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus OrderStatus { get; set; } 
        public DateTime CreatedDate { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
