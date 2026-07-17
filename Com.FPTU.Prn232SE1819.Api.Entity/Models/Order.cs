using System;
using System.Collections.Generic;

namespace Com.FPTU.Prn232SE1819.Api.Entity.Models;

public partial class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalPrice { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
