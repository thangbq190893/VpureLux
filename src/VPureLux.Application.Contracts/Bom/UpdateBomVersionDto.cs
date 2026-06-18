using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPureLux.Bom;

public class UpdateBomVersionDto
{
    [Required]
    [MinLength(1)]
    public List<CreateBomItemDto> Items { get; set; } = new();
}
