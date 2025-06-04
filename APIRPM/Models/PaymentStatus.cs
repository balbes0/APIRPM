using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class PaymentStatus
{
    public int IdPaymentStatus { get; set; }

    public string PaymentStatusName { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
