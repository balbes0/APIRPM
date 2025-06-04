using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class Payment
{
    public int IdPayment { get; set; }

    public int OrderId { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public int PaymentMethodId { get; set; }

    public int PaymentStatusId { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual PaymentStatus PaymentStatus { get; set; } = null!;
}
