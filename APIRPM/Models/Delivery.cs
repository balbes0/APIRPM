using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class Delivery
{
    public int IdDelivery { get; set; }

    public int OrderId { get; set; }

    public string DeliveryAddress { get; set; } = null!;

    public DateOnly? DeliveryDate { get; set; }

    public int DeliveryStatusId { get; set; }

    public virtual DeliveryStatus DeliveryStatus { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
