using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class Order
{
    public int IdOrder { get; set; }

    public int UserId { get; set; }

    public DateOnly? OrderDate { get; set; }

    public decimal TotalSum { get; set; }

    public int StatusId { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PosOrder> PosOrders { get; set; } = new List<PosOrder>();

    public virtual StatusOrder Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
