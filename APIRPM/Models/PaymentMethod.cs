using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class PaymentMethod
{
    public int IdPaymentMethod { get; set; }

    public string? PaymentMethodName { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
