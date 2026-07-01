using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Localization;
using VPureLux.Pricing;
using VPureLux.Web.Pages.Bom;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class BomPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public void Bom_Terminology_Should_Use_Product_Title_And_Keep_Line_Label_As_Vat_Tu()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var localizationSource = File.ReadAllText(GetRepoFilePath("src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json"));

        localizer["Menu:Bom"].Value.ShouldBe("Định mức sản phẩm (BOM)");
        localizer["Bom:Title"].Value.ShouldBe("Định mức sản phẩm (BOM)");
        localizer["Bom:Component"].Value.ShouldBe("Vật tư");
        localizationSource.ShouldNotContain("Định mức vật" + " tư (BOM)");
        localizationSource.ShouldNotContain("Linh " + "kiện");
        localizationSource.ShouldNotContain("linh " + "kiện");
    }

    [Fact]
    public async Task Bom_Index_Should_Render_Searchable_Product_Table_With_Row_Actions()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await CreateProductAsync("BOM-IDX", "BOM Product Selector");
        var component = await CreateComponentAsync("BOM-IDX-C", "BOM Index Component");
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, BomInput(component.Id, DateTime.Today));
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync("/Bom"));
        var filteredHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom?SearchTerm={product.Code}"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/Index.cshtml"));

        html.ShouldContain(localizer["Bom:Title"].Value);
        html.ShouldContain("data-bom-search-form");
        html.ShouldContain("data-bom-summary-table");
        html.ShouldContain("name=\"SearchTerm\"");
        html.ShouldContain(localizer["Bom:VersionCount"].Value);
        filteredHtml.ShouldContain(product.Code);
        filteredHtml.ShouldContain(product.Name);
        filteredHtml.ShouldContain("data-bom-summary-row");
        html.ShouldContain(localizer["Bom:OpenHistory"].Value);
        filteredHtml.ShouldContain(localizer["Bom:CreateVersionForProduct"].Value);
        filteredHtml.ShouldContain(localizer["Bom:ViewCurrentVersion"].Value);
        filteredHtml.ShouldContain($"href=\"/Bom/Product/{product.Id}");
        filteredHtml.ShouldContain($"href=\"/Bom/Create/{product.Id}");
        filteredHtml.ShouldContain($"/Bom/Details/{bom.Id}");
        pageSource.ShouldNotContain("asp-for=\"ProductId\" asp-items=");
        pageSource.ShouldNotContain("Bom:SelectProduct");
        html.ShouldNotContain("type=\"text\" id=\"ProductId\"");
    }

    [Fact]
    public async Task Bom_Create_Should_Render_Component_Selector_And_External_Script()
    {
        var product = await CreateProductAsync("BOM-CRT-P", "BOM Create Product");
        var component = await CreateComponentAsync("BOM-CRT-C", "BOM Create Component");

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Create/{product.Id}"));

        html.ShouldContain($"{component.Code} - {component.Name}");
        html.ShouldContain("name=\"Items[0].ComponentId\"");
        html.ShouldContain("data-bom-create-product-context");
        html.ShouldContain($"{product.Code} - {product.Name}");
        html.ShouldContain("vpl-line-editor-table bom-items-table");
        html.ShouldContain("form-select form-select-sm component-id");
        html.ShouldContain("vpl-line-editor-icon-button");

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/Create.cshtml"));
        pageSource.ShouldContain("@section styles");
        pageSource.ShouldContain("<abp-style src=\"/Pages/Shared/LineEditors.css\" />");
        pageSource.ShouldContain("data-bom-create-product-context");
        pageSource.ShouldContain("data-line-editor-row");
        pageSource.ShouldContain("data-dynamic-select2=\"disabled\"");
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Shared/DynamicRowSelects.js\" />");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Bom/BomItems.js\" />");
        pageSource.ShouldNotContain("<script>");

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/BomItems.js"));
        scriptSource.ShouldContain("component.innerHTML = sourceComponent.innerHTML");
        scriptSource.ShouldContain("component.name = 'Items[' + index + '].ComponentId'");
        scriptSource.ShouldContain("quantity.name = 'Items[' + index + '].Quantity'");
        scriptSource.ShouldContain("component.id = 'Items_' + index + '__ComponentId'");
        scriptSource.ShouldContain("quantity.id = 'Items_' + index + '__Quantity'");
        scriptSource.ShouldContain("initializeSelects(row)");
        scriptSource.ShouldContain("vplDynamicRowSelects");
    }

    [Fact]
    public async Task Bom_Product_And_Details_Should_Render_Catalog_Labels_Formatted_Dates_And_Integer_Quantity()
    {
        var product = await CreateProductAsync("BOM-LBL-P", "BOM Label Product");
        var component = await CreateComponentAsync("BOM-LBL-C", "BOM Label Component");
        var effectiveFrom = new DateTime(2026, 6, 18);
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = effectiveFrom,
            Items =
            [
                new CreateBomItemDto
                {
                    ComponentId = component.Id,
                    Quantity = 1
                }
            ]
        });

        var productHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));
        var detailsHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Details/{bom.Id}"));

        productHtml.ShouldContain($"{product.Code} - {product.Name}");
        detailsHtml.ShouldContain($"{product.Code} - {product.Name}");
        detailsHtml.ShouldContain($"{component.Code} - {component.Name}");
        productHtml.ShouldContain("18/06/2026");
        detailsHtml.ShouldContain("18/06/2026");
        detailsHtml.ShouldContain(">1</td>");
        detailsHtml.ShouldNotContain("1,0000");
        detailsHtml.ShouldNotContain("1.0000");
    }

    [Fact]
    public async Task Bom_Product_Should_Render_Publish_Archive_Confirmation_And_Notification_Hooks()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await CreateProductAsync("BOM-ACT-P", "BOM Action Product");
        var component = await CreateComponentAsync("BOM-ACT-C", "BOM Action Component");
        var bomService = GetRequiredService<IBomAppService>();
        var draft = await bomService.CreateAsync(product.Id, BomInput(component.Id, DateTime.Today));

        var draftHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));
        draftHtml.ShouldContain("data-bom-product");
        draftHtml.ShouldContain("data-bom-action-form");
        draftHtml.ShouldContain(localizer["Bom:ConfirmPublish"].Value);

        await bomService.PublishAsync(draft.Id);
        var publishedHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));
        publishedHtml.ShouldContain(localizer["Bom:ConfirmArchive"].Value);

        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/Product.cshtml"));
        pageSource.ShouldContain("@section scripts");
        pageSource.ShouldContain("<abp-script src=\"/Pages/Bom/BomProduct.js\" />");
        pageSource.ShouldNotContain("<script>");
        pageSource.ShouldNotContain("<script src=");
        pageSource.ShouldNotContain("<abp-button href=");
        pageSource.ShouldNotContain("href=\"/");

        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/BomProduct.js"));
        scriptSource.ShouldContain("abp.message.confirm");
        scriptSource.ShouldContain("abp.notify.success");
        scriptSource.ShouldContain("abp.ui.setBusy");
        scriptSource.ShouldContain("dataset.confirmed");
    }

    [Fact]
    public async Task Bom_Product_Should_Render_Product_And_Existing_Pricing_Context()
    {
        var product = await CreateProductAsync("BOM-CTX-P", "BOM Context Product");
        var component = await CreateComponentAsync("BOM-CTX-C", "BOM Context Component");
        await GetRequiredService<IComponentSuggestedSellingPriceAppService>()
            .CreateAsync(component.Id, new CreateComponentSuggestedSellingPriceVersionDto
            {
                Price = 50000m,
                Reason = "Giá vật tư cho ngữ cảnh BOM",
                EffectiveFrom = DateTime.Today
            });
        await GetRequiredService<IProductSuggestedPriceAppService>()
            .CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
            {
                Price = 120000m,
                Reason = "Giá sản phẩm cho ngữ cảnh BOM",
                EffectiveFrom = DateTime.Today
            });

        var bom = await GetRequiredService<IBomAppService>().CreateAsync(
            product.Id,
            BomInput(component.Id, DateTime.Today, quantity: 2));
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Product/{product.Id}"));

        html.ShouldContain($"Sản phẩm: {product.Code} - {product.Name}");
        html.ShouldContain("Giá bán đề xuất hiện tại");
        html.ShouldContain("Giá cấu thành vật tư");
        html.ShouldContain("120.000 VND");
        html.ShouldContain("100.000 VND");
        html.ShouldContain("Đã có định mức công bố");
    }

    [Fact]
    public async Task Bom_Product_PageModel_Should_Use_Scoped_Product_Pricing_Context()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/Product.cshtml.cs"));

        pageSource.ShouldContain("FindMapAsync([ProductId]");
        pageSource.ShouldNotContain("_productPricingContextAppService.GetListAsync()");
    }

    [Fact]
    public async Task Bom_Edit_Should_Preserve_Component_Selections_And_Quantities()
    {
        var product = await CreateProductAsync("BOM-EDIT-P", "BOM Edit Product");
        var firstComponent = await CreateComponentAsync("BOM-EDIT-C1", "BOM Edit Component 1");
        var secondComponent = await CreateComponentAsync("BOM-EDIT-C2", "BOM Edit Component 2");
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Today,
            Items =
            [
                new CreateBomItemDto { ComponentId = firstComponent.Id, Quantity = 2 },
                new CreateBomItemDto { ComponentId = secondComponent.Id, Quantity = 3 }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Edit/{bom.Id}"));

        html.ShouldContain($"{firstComponent.Code} - {firstComponent.Name}");
        html.ShouldContain($"{secondComponent.Code} - {secondComponent.Name}");
        html.ShouldContain("name=\"Items[0].ComponentId\"");
        html.ShouldContain("name=\"Items[1].ComponentId\"");
        html.ShouldContain("name=\"Items[0].Quantity\"");
        html.ShouldContain("name=\"Items[1].Quantity\"");
        html.ShouldContain("value=\"2\"");
        html.ShouldContain("value=\"3\"");
    }

    [Fact]
    public async Task Bom_Edit_Should_Render_Compact_Multi_Line_Layout_Without_Duplicate_Selects()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await CreateProductAsync("BOM-EDIT-CMP-P", "BOM Edit Compact Product");
        var firstComponent = await CreateComponentAsync("BOM-EDIT-CMP-C1", "BOM Edit Compact Component 1");
        var secondComponent = await CreateComponentAsync("BOM-EDIT-CMP-C2", "BOM Edit Compact Component 2");
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Today,
            Items =
            [
                new CreateBomItemDto { ComponentId = firstComponent.Id, Quantity = 2 },
                new CreateBomItemDto { ComponentId = secondComponent.Id, Quantity = 3 }
            ]
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Edit/{bom.Id}"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/Edit.cshtml"));
        var scriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Bom/BomItems.js"));
        var sharedScriptSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js"));

        html.ShouldContain("vpl-line-editor-table bom-items-table");
        html.ShouldContain("form-select form-select-sm component-id");
        html.ShouldContain("form-control form-control-sm quantity");
        html.ShouldContain("vpl-line-editor-icon-button");
        CountOccurrences(html, "name=\"Items[0].ComponentId\"").ShouldBe(1);
        CountOccurrences(html, "name=\"Items[1].ComponentId\"").ShouldBe(1);
        CountOccurrences(html, "<select class=\"form-select form-select-sm component-id\"").ShouldBe(2);
        CountLiveRowsWithExactlyOneSelect(html, "<tr class=\"bom-item\" data-line-editor-row>").ShouldBe(2);
        CountOccurrences(html, localizer["Bom:SelectComponent"].Value).ShouldBe(2);
        html.ShouldContain("data-dynamic-select2=\"disabled\"");
        html.ShouldNotContain("select2-container");

        pageSource.ShouldContain("<abp-style src=\"/Pages/Shared/LineEditors.css\" />");
        pageSource.ShouldContain("data-line-editor-row");
        pageSource.ShouldContain("data-dynamic-select2=\"disabled\"");
        pageSource.ShouldContain("vpl-line-editor-col-main");
        pageSource.ShouldContain("vpl-line-editor-col-number");
        pageSource.ShouldContain("vpl-line-editor-col-action");

        scriptSource.ShouldContain("component.name = 'Items[' + index + '].ComponentId'");
        scriptSource.ShouldContain("quantity.name = 'Items[' + index + '].Quantity'");
        scriptSource.ShouldContain("dynamicRows.stripSelect2Enhancements(row)");
        scriptSource.ShouldContain("initializeSelects(row)");
        sharedScriptSource.ShouldContain("data-dynamic-select2=\"disabled\"");
        sharedScriptSource.ShouldContain("setControlsDisabled(template, true)");
        sharedScriptSource.ShouldContain("template.classList.add('d-none')");
        localizer["Bom:Component"].Value.ShouldBe("Vật tư");
    }

    [Fact]
    public async Task Bom_Edit_OnPostAsync_Should_Save_Normal_Path_Without_Concurrency_Error()
    {
        var product = await CreateProductAsync("BOM-EDIT-SAVE-P", "BOM Edit Save Product");
        var firstComponent = await CreateComponentAsync("BOM-EDIT-SAVE-C1", "BOM Edit Save Component 1");
        var secondComponent = await CreateComponentAsync("BOM-EDIT-SAVE-C2", "BOM Edit Save Component 2");
        var bomService = GetRequiredService<IBomAppService>();
        var bom = await bomService.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Today,
            Items = [new CreateBomItemDto { ComponentId = firstComponent.Id, Quantity = 1 }]
        });
        var model = new global::VPureLux.Web.Pages.Bom.EditModel(bomService, GetRequiredService<IComponentAppService>())
        {
            Id = bom.Id,
            Items =
            [
                new BomItemSelectionModel { ComponentId = firstComponent.Id, Quantity = 2 },
                new BomItemSelectionModel { ComponentId = secondComponent.Id, Quantity = 3 }
            ]
        };
        SetPageContext(model);

        var result = await model.OnPostAsync();

        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        redirect.PageName.ShouldBe("/Bom/Details");
        var updated = await bomService.GetAsync(bom.Id);
        updated.Items.Count.ShouldBe(2);
        updated.Items.ShouldContain(x => x.ComponentId == firstComponent.Id && x.Quantity == 2);
        updated.Items.ShouldContain(x => x.ComponentId == secondComponent.Id && x.Quantity == 3);
    }

    [Fact]
    public async Task Bom_Edit_OnPostAsync_Should_Show_Friendly_Error_When_Concurrency_Blocks_Save()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var bomService = Substitute.For<IBomAppService>();
        bomService.UpdateAsync(Arg.Any<Guid>(), Arg.Any<UpdateBomVersionDto>())
            .Returns<BomVersionDto>(_ => throw new AbpDbConcurrencyException("Expected test concurrency."));
        var components = Substitute.For<IComponentAppService>();
        components.GetListAsync(Arg.Any<GetComponentListInput>())
            .Returns(new Volo.Abp.Application.Dtos.PagedResultDto<ComponentDto>());
        var model = new global::VPureLux.Web.Pages.Bom.EditModel(bomService, components)
        {
            Id = Guid.NewGuid(),
            Items = [new BomItemSelectionModel { ComponentId = Guid.NewGuid(), Quantity = 1 }]
        };
        SetPageContext(model);

        var result = await model.OnPostAsync();

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[string.Empty]!.Errors
            .ShouldContain(x => x.ErrorMessage == localizer["Bom:ConcurrencyError"].Value);
    }

    [Fact]
    public async Task Bom_Product_OnPostPublishAsync_Should_Show_Friendly_Error_When_Business_Rule_Blocks_Publish()
    {
        var localizer = GetRequiredService<IStringLocalizer<VPureLuxResource>>();
        var product = await CreateProductAsync("BOM-PUBLISH-P", "BOM Publish Product");
        var firstComponent = await CreateComponentAsync("BOM-PUBLISH-C1", "BOM Publish Component 1");
        var secondComponent = await CreateComponentAsync("BOM-PUBLISH-C2", "BOM Publish Component 2");
        var bomService = GetRequiredService<IBomAppService>();
        var first = await bomService.CreateAsync(product.Id, BomInput(firstComponent.Id, DateTime.Today));
        await bomService.PublishAsync(first.Id);
        var draft = await bomService.CreateAsync(product.Id, BomInput(secondComponent.Id, DateTime.Today.AddDays(1)));
        var model = new ProductModel(
            bomService,
            GetRequiredService<IProductAppService>(),
            GetRequiredService<IProductPricingContextLookupService>(),
            GetRequiredService<IAuthorizationService>())
        {
            ProductId = product.Id
        };
        SetPageContext(model);

        var result = await model.OnPostPublishAsync(draft.Id);

        result.ShouldBeOfType<PageResult>();
        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[string.Empty]!.Errors
            .ShouldContain(x => x.ErrorMessage == localizer[VPureLuxDomainErrorCodes.OnlyOneActiveBomAllowed].Value);
        model.Versions.Count.ShouldBeGreaterThanOrEqualTo(2);
        (await bomService.GetAsync(draft.Id)).Status.ShouldBe(BomStatus.Draft);
    }

    [Fact]
    public async Task Bom_Create_And_Clone_Should_Use_Vietnamese_Date_Text()
    {
        var product = await CreateProductAsync("BOM-DATE-P", "BOM Date Product");
        var component = await CreateComponentAsync("BOM-DATE-C", "BOM Date Component");
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Today,
            Items =
            [
                new CreateBomItemDto
                {
                    ComponentId = component.Id,
                    Quantity = 1
                }
            ]
        });

        var createHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync($"/Bom/Create/{product.Id}"));

        createHtml.ShouldContain("placeholder=\"dd/MM/yyyy\"");
        createHtml.ShouldNotContain("type=\"date\"");

        var cloneModel = new global::VPureLux.Web.Pages.Bom.CloneModel(GetRequiredService<IBomAppService>())
        {
            Id = bom.Id,
            EffectiveFromText = "18/06/2026"
        };

        var result = await cloneModel.OnPostAsync();
        var redirect = result.ShouldBeOfType<RedirectToPageResult>();
        var cloneId = redirect.RouteValues!["id"].ShouldBeOfType<Guid>();
        var clone = await GetRequiredService<IBomAppService>().GetAsync(cloneId);

        clone.EffectiveFrom.ShouldBe(new DateTime(2026, 6, 18));
    }

    private async Task<ProductDto> CreateProductAsync(string prefix, string name)
    {
        return await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = prefix + Guid.NewGuid().ToString("N")[..8],
            Name = name
        });
    }

    private async Task<ComponentDto> CreateComponentAsync(string prefix, string name)
    {
        return await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = prefix + Guid.NewGuid().ToString("N")[..8],
            Name = name,
            Unit = "pcs"
        });
    }

    private static CreateBomVersionDto BomInput(Guid componentId, DateTime effectiveFrom, decimal quantity = 1) => new()
    {
        EffectiveFrom = effectiveFrom,
        Items =
        [
            new CreateBomItemDto
            {
                ComponentId = componentId,
                Quantity = quantity
            }
        ]
    };

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private static int CountLiveRowsWithExactlyOneSelect(string html, string rowMarker)
    {
        var rowCount = 0;
        var index = 0;

        while ((index = html.IndexOf(rowMarker, index, StringComparison.Ordinal)) >= 0)
        {
            var end = html.IndexOf("</tr>", index, StringComparison.Ordinal);
            end.ShouldBeGreaterThan(index);

            var rowHtml = html[index..end];
            CountOccurrences(rowHtml, "<select ").ShouldBe(1);
            CountOccurrences(rowHtml, "data-dynamic-row-template").ShouldBe(0);
            rowCount++;
            index = end + "</tr>".Length;
        }

        return rowCount;
    }

    private void SetPageContext(PageModel model)
    {
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = GetRequiredService<IServiceProvider>()
            }
        };

        if (model is global::VPureLux.Web.Pages.VPureLuxPageModel vplModel)
        {
            vplModel.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }
    }

    private static string GetRepoFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
