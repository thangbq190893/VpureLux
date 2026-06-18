using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogImagePagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Product_List_Should_Render_Thumbnail_And_Placeholder_Lazily()
    {
        var service = GetRequiredService<IProductAppService>();
        var withoutImage = await service.CreateAsync(new CreateProductDto { Code = Unique("PAGE-P0"), Name = "No Image" });
        var withImage = await service.CreateAsync(new CreateProductDto { Code = Unique("PAGE-P1"), Name = "With Image" });
        await service.SetImageAsync(withImage.Id, CatalogImageTestData.Png());

        var html = await GetResponseAsStringAsync("/Catalog/Products");

        html.ShouldContain($"/api/catalog/products/{withImage.Id}/thumbnail");
        html.ShouldContain("loading=\"lazy\"");
        html.ShouldContain("No image");
    }

    [Fact]
    public async Task Component_List_Should_Render_Thumbnail_And_Placeholder_Lazily()
    {
        var service = GetRequiredService<IComponentAppService>();
        await service.CreateAsync(new CreateComponentDto { Code = Unique("PAGE-C0"), Name = "No Image", Unit = "Piece" });
        var withImage = await service.CreateAsync(new CreateComponentDto { Code = Unique("PAGE-C1"), Name = "With Image", Unit = "Piece" });
        await service.SetImageAsync(withImage.Id, CatalogImageTestData.Png());

        var html = await GetResponseAsStringAsync("/Catalog/Components");

        html.ShouldContain($"/api/catalog/components/{withImage.Id}/thumbnail");
        html.ShouldContain("loading=\"lazy\"");
    }

    [Fact]
    public void Razor_Pages_Should_Provide_Upload_Preview_Replace_And_Remove_Controls()
    {
        var root = FindRepositoryRoot();
        foreach (var item in new[] { "Products", "Components" })
        {
            var create = File.ReadAllText(Path.Combine(root, "src", "VPureLux.Web", "Pages", "Catalog", item, "Create.cshtml"));
            var edit = File.ReadAllText(Path.Combine(root, "src", "VPureLux.Web", "Pages", "Catalog", item, "Edit.cshtml"));

            create.ShouldContain("multipart/form-data");
            create.ShouldContain("data-catalog-image-input");
            edit.ShouldContain("Catalog:ReplaceImage");
            edit.ShouldContain("asp-page-handler=\"RemoveImage\"");
            edit.ShouldNotContain("ImageBase64");
        }
    }

    [Fact]
    public void Image_Page_Models_Should_Use_Existing_Authorized_Create_And_Edit_Permissions()
    {
        typeof(Web.Pages.Catalog.Products.CreateModel).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()!
            .Policy.ShouldBe(Permissions.VPureLuxPermissions.Catalog.Products.Create);
        typeof(Web.Pages.Catalog.Products.EditModel).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()!
            .Policy.ShouldBe(Permissions.VPureLuxPermissions.Catalog.Products.Edit);
        typeof(Web.Pages.Catalog.Components.CreateModel).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()!
            .Policy.ShouldBe(Permissions.VPureLuxPermissions.Catalog.Components.Create);
        typeof(Web.Pages.Catalog.Components.EditModel).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()!
            .Policy.ShouldBe(Permissions.VPureLuxPermissions.Catalog.Components.Edit);
    }

    private static string FindRepositoryRoot()
    {
        var path = AppContext.BaseDirectory;
        while (path != null && !File.Exists(Path.Combine(path, "VPureLux.slnx")))
        {
            path = Directory.GetParent(path)?.FullName;
        }

        return path ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
