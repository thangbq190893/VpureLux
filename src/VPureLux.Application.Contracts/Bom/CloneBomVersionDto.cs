using System;
using System.ComponentModel.DataAnnotations;

namespace VPureLux.Bom;

public class CloneBomVersionDto
{
    [Required]
    public DateTime EffectiveFrom { get; set; }
}
