using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaBot.Api.Models.Domain;

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderType
{
    Market,
    Limit,
    StopLimit
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    public string? BitgetOrderId { get; set; }

    [Required, MaxLength(20)]
    public string Symbol { get; set; } = "";

    public OrderSide Side { get; set; }
    public OrderType OrderType { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal? Price { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal Quantity { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    [Column(TypeName = "decimal(18,8)")]
    public decimal FilledQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
