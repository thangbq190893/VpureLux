using System;
using System.ComponentModel.DataAnnotations;

namespace VPureLux.Bom;

public class CreateBomItemDto
{
    [Required]
    public Guid ComponentId { get; set; }

    [Range(
        typeof(decimal),
        "1",
        "99999999999999",
        ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }
}
