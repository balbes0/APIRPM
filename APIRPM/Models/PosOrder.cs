using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class PosOrder
{
    public int IdPos { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Catalog Product { get; set; } = null!;
}
