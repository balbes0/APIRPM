using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class DeliveryStatus
{
    public int IdDeliveryStatus { get; set; }

    public string DeliveryStatusName { get; set; } = null!;

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
