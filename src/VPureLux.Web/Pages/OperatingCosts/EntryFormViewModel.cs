using System.Collections.Generic;
using VPureLux.OperatingCosts;

namespace VPureLux.Web.Pages.OperatingCosts;

public class EntryFormViewModel
{
    public UpdateOperatingCostEntryDto Input { get; set; } = new();
    public List<OperatingCostCategoryDto> Categories { get; set; } = [];
}
