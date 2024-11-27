using System;
using System.Collections.Generic;

namespace SellApp.Model;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Barcode { get; set; }

    public string? Unit { get; set; }

    public decimal Price { get; set; }

    public string? Note { get; set; }
}
