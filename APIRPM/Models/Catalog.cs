using System;
using System.Collections.Generic;

namespace APIRPM.Models;

public partial class Catalog
{
    public int IdProduct { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int? Weight { get; set; }

    public int? Stock { get; set; }

    public string? CategoryName { get; set; }

    public string? PathToImage { get; set; }

    public virtual ICollection<PosOrder> PosOrders { get; set; } = new List<PosOrder>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
