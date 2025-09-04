namespace ProductOrderAPI.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
