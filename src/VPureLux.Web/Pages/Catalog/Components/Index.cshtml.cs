using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Components;

public class IndexModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    public IReadOnlyList<ComponentDto> Components { get; private set; } = Array.Empty<ComponentDto>();
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }

    public IndexModel(IComponentAppService componentAppService, IAuthorizationService authorizationService)
    {
        _componentAppService = componentAppService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        var result = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            Keyword = Keyword,
            MaxResultCount = 100
        });

        Components = result.Items;
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Components.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Components.Edit)).Succeeded;
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _componentAppService.DeactivateAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _componentAppService.ActivateAsync(id);
        return RedirectToPage();
    }
}
