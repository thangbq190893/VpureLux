using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.OperatingCosts;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Validation;
using Xunit;

namespace VPureLux.EntityFrameworkCore.OperatingCosts;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class OperatingCostAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IOperatingCostAppService _service;

    public OperatingCostAppServiceTests()
    {
        _service = GetRequiredService<IOperatingCostAppService>();
    }

    [Fact]
    public async Task Should_Create_Income_Expense_And_Summarize_Month_Debt()
    {
        var expenseCategory = await _service.CreateCategoryAsync(new CreateOperatingCostCategoryDto
        {
            Code = Unique("EXP"),
            Name = "Tiếp khách",
            Direction = OperatingCostDirection.Expense
        });
        var incomeCategory = await _service.CreateCategoryAsync(new CreateOperatingCostCategoryDto
        {
            Code = Unique("INC"),
            Name = "Thu khác",
            Direction = OperatingCostDirection.Income
        });

        await _service.CreateEntryAsync(new CreateOperatingCostEntryDto
        {
            EntryDate = new DateTime(2026, 8, 10),
            Direction = OperatingCostDirection.Expense,
            CategoryId = expenseCategory.Id,
            Amount = 300_000,
            PaymentStatus = OperatingCostPaymentStatus.Paid,
            Description = "Đi ăn uống tiếp khách",
            Counterparty = "Nhà hàng"
        });
        await _service.CreateEntryAsync(new CreateOperatingCostEntryDto
        {
            EntryDate = new DateTime(2026, 8, 11),
            Direction = OperatingCostDirection.Expense,
            CategoryId = expenseCategory.Id,
            Amount = 1_200_000,
            PaymentStatus = OperatingCostPaymentStatus.Unpaid,
            DueDate = new DateTime(2026, 8, 31),
            Description = "Mua trả góp trang thiết bị"
        });
        await _service.CreateEntryAsync(new CreateOperatingCostEntryDto
        {
            EntryDate = new DateTime(2026, 8, 12),
            Direction = OperatingCostDirection.Income,
            CategoryId = incomeCategory.Id,
            Amount = 500_000,
            PaymentStatus = OperatingCostPaymentStatus.Unpaid,
            Description = "Thu hoàn ứng"
        });

        var summary = await _service.GetSummaryAsync(new GetOperatingCostEntryListInput
        {
            FromDate = new DateTime(2026, 8, 1),
            ToDate = new DateTime(2026, 8, 31)
        });

        summary.TotalExpense.ShouldBe(1_500_000);
        summary.TotalIncome.ShouldBe(500_000);
        summary.NetAmount.ShouldBe(-1_000_000);
        summary.UnpaidPayable.ShouldBe(1_200_000);
        summary.UnpaidReceivable.ShouldBe(500_000);
    }

    [Fact]
    public async Task Should_Block_Invalid_Amount_And_Category_Delete_When_In_Use()
    {
        var category = await _service.CreateCategoryAsync(new CreateOperatingCostCategoryDto
        {
            Code = Unique("OPC"),
            Name = "Chi phí kiểm thử",
            Direction = OperatingCostDirection.Expense
        });

        await Should.ThrowAsync<AbpValidationException>(() => _service.CreateEntryAsync(new CreateOperatingCostEntryDto
        {
            EntryDate = DateTime.Today,
            Direction = OperatingCostDirection.Expense,
            CategoryId = category.Id,
            Amount = 0,
            PaymentStatus = OperatingCostPaymentStatus.Paid,
            Description = "Không hợp lệ"
        }));

        await _service.CreateEntryAsync(new CreateOperatingCostEntryDto
        {
            EntryDate = DateTime.Today,
            Direction = OperatingCostDirection.Expense,
            CategoryId = category.Id,
            Amount = 1,
            PaymentStatus = OperatingCostPaymentStatus.Paid,
            Description = "Có phát sinh"
        });

        (await Should.ThrowAsync<BusinessException>(() => _service.DeleteCategoryAsync(category.Id)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.OperatingCostCategoryInUse);
    }

    [Fact]
    public async Task Should_Define_Permissions_Protect_Service_And_Map_Ef_Metadata()
    {
        var permissions = GetRequiredService<IPermissionDefinitionManager>();
        foreach (var permission in new[]
                 {
                     VPureLuxPermissions.OperatingCosts.View,
                     VPureLuxPermissions.OperatingCosts.ManageEntries,
                     VPureLuxPermissions.OperatingCosts.ManageCategories,
                     VPureLuxPermissions.OperatingCosts.Delete
                 })
        {
            (await permissions.GetAsync(permission)).ShouldNotBeNull();
        }

        Permission(typeof(OperatingCostAppService)).ShouldBe(VPureLuxPermissions.OperatingCosts.Default);
        Permission(nameof(OperatingCostAppService.GetEntryListAsync)).ShouldBe(VPureLuxPermissions.OperatingCosts.View);
        Permission(nameof(OperatingCostAppService.CreateEntryAsync)).ShouldBe(VPureLuxPermissions.OperatingCosts.ManageEntries);
        Permission(nameof(OperatingCostAppService.DeleteEntryAsync)).ShouldBe(VPureLuxPermissions.OperatingCosts.Delete);

        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var entry = db.Model.FindEntityType(typeof(OperatingCostEntry))!;
            entry.GetTableName().ShouldBe("AppOperatingCostEntries");
            entry.FindProperty(nameof(OperatingCostEntry.Amount))!.GetPrecision().ShouldBe(18);
            entry.FindProperty(nameof(OperatingCostEntry.Amount))!.GetScale().ShouldBe(2);
            entry.GetForeignKeys().ShouldAllBe(x => x.DeleteBehavior == DeleteBehavior.Restrict);
            db.Model.FindEntityType(typeof(OperatingCostCategory))!
                .GetIndexes()
                .Single(x => x.GetDatabaseName() == "UX_OperatingCostCategories_Code")
                .IsUnique.ShouldBeTrue();
        });
    }

    private static string? Permission(MemberInfo member) => member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;

    private static string? Permission(string method) => Permission(typeof(OperatingCostAppService).GetMethod(method)!);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(32, prefix.Length + 9)];
}
