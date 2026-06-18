using System;

namespace VPureLux.Web.Pages.Bom;

public class BomItemSelectionModel
{
    public Guid? ComponentId { get; set; }
    public decimal Quantity { get; set; } = 1;
}
