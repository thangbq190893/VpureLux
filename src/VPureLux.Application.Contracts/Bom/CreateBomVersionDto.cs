using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPureLux.Bom;

public class CreateBomVersionDto
{
    [Required]
    public DateTime EffectiveFrom { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateBomItemDto> Items { get; set; } = new();
}
