using System;
using System.Threading.Tasks;
using global::VPureLux.Catalog.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Components;

[Authorize(VPureLuxPermissions.Catalog.Components.View)]
public class DetailsModalModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public ComponentDto Component { get; private set; } = new();

    public DetailsModalModel(IComponentAppService componentAppService)
    {
        _componentAppService = componentAppService;
    }

    public async Task OnGetAsync()
    {
        Component = await _componentAppService.GetAsync(Id);
    }
}
