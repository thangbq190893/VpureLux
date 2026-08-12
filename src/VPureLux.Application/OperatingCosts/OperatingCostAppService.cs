using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.OperatingCosts;

[Authorize(VPureLuxPermissions.OperatingCosts.Default)]
public class OperatingCostAppService : ApplicationService, IOperatingCostAppService
{
    private readonly IRepository<OperatingCostCategory, Guid> _categoryRepository;
    private readonly IRepository<OperatingCostEntry, Guid> _entryRepository;
    private readonly OperatingCostManager _manager;

    public OperatingCostAppService(
        IRepository<OperatingCostCategory, Guid> categoryRepository,
        IRepository<OperatingCostEntry, Guid> entryRepository,
        OperatingCostManager manager)
    {
        _categoryRepository = categoryRepository;
        _entryRepository = entryRepository;
        _manager = manager;
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.View)]
    public async Task<PagedResultDto<OperatingCostCategoryDto>> GetCategoryListAsync(GetOperatingCostCategoryListInput input)
    {
        var query = ApplyCategoryFilter(await _categoryRepository.GetQueryableAsync(), input);
        var totalCount = await AsyncExecuter.LongCountAsync(query);
        var categories = await AsyncExecuter.ToListAsync(ApplyCategorySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        return new PagedResultDto<OperatingCostCategoryDto>(
            totalCount,
            categories.Select(ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.View)]
    public async Task<List<OperatingCostCategoryDto>> GetActiveCategoriesAsync(OperatingCostDirection? direction = null)
    {
        var query = (await _categoryRepository.GetQueryableAsync())
            .Where(x => x.IsActive);

        if (direction.HasValue)
        {
            query = query.Where(x => x.Direction == direction.Value);
        }

        var categories = await AsyncExecuter.ToListAsync(query
            .OrderBy(x => x.Direction)
            .ThenBy(x => x.Name));

        return categories.Select(ToDto).ToList();
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.View)]
    public async Task<OperatingCostCategoryDto> GetCategoryAsync(Guid id)
    {
        return ToDto(await GetCategoryEntityAsync(id));
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.ManageCategories)]
    public async Task<OperatingCostCategoryDto> CreateCategoryAsync(CreateOperatingCostCategoryDto input)
    {
        var category = await _manager.CreateCategoryAsync(input.Code, input.Name, input.Direction);
        category.UpdateInfo(input.Name, input.Direction, input.IsActive);
        await _categoryRepository.InsertAsync(category, autoSave: true);
        return ToDto(category);
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.ManageCategories)]
    public async Task<OperatingCostCategoryDto> UpdateCategoryAsync(Guid id, UpdateOperatingCostCategoryDto input)
    {
        var category = await GetCategoryEntityAsync(id);
        input.Code = OperatingCostCategory.NormalizeCode(input.Code);
        await _manager.EnsureCategoryCodeCanBeUsedAsync(id, input.Code);

        category.ChangeCode(input.Code);
        category.UpdateInfo(input.Name, input.Direction, input.IsActive);
        await _categoryRepository.UpdateAsync(category, autoSave: true);
        return ToDto(category);
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.ManageCategories)]
    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await GetCategoryEntityAsync(id);
        await _manager.EnsureCanDeleteCategoryAsync(id);
        await _categoryRepository.DeleteAsync(category, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.View)]
    public async Task<PagedResultDto<OperatingCostEntryDto>> GetEntryListAsync(GetOperatingCostEntryListInput input)
    {
        var query = ApplyEntryFilter(await _entryRepository.GetQueryableAsync(), input);
        var totalCount = await AsyncExecuter.LongCountAsync(query);
        var entries = await AsyncExecuter.ToListAsync(ApplyEntrySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        var categoryMap = await GetCategoryCodeMapAsync(entries.Select(x => x.CategoryId));

        return new PagedResultDto<OperatingCostEntryDto>(
            totalCount,
            entries.Select(entry => ToDto(entry, categoryMap)).ToList());
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.View)]
    public async Task<OperatingCostSummaryDto> GetSummaryAsync(GetOperatingCostEntryListInput input)
    {
        var entries = await AsyncExecuter.ToListAsync(ApplyEntryFilter(await _entryRepository.GetQueryableAsync(), input));

        var income = entries
            .Where(x => x.Direction == OperatingCostDirection.Income)
            .Sum(x => x.Amount);
        var expense = entries
            .Where(x => x.Direction == OperatingCostDirection.Expense)
            .Sum(x => x.Amount);
        var unpaidReceivable = entries
            .Where(x => x.Direction == OperatingCostDirection.Income && x.PaymentStatus == OperatingCostPaymentStatus.Unpaid)
            .Sum(x => x.Amount);
        var unpaidPayable = entries
            .Where(x => x.Direction == OperatingCostDirection.Expense && x.PaymentStatus == OperatingCostPaymentStatus.Unpaid)
            .Sum(x => x.Amount);

        return new OperatingCostSummaryDto
        {
            TotalIncome = income,
            TotalExpense = expense,
            NetAmount = income - expense,
            UnpaidReceivable = unpaidReceivable,
            UnpaidPayable = unpaidPayable
        };
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.View)]
    public async Task<OperatingCostEntryDto> GetEntryAsync(Guid id)
    {
        var entry = await GetEntryEntityAsync(id);
        var categoryMap = await GetCategoryCodeMapAsync([entry.CategoryId]);
        return ToDto(entry, categoryMap);
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.ManageEntries)]
    public async Task<OperatingCostEntryDto> CreateEntryAsync(CreateOperatingCostEntryDto input)
    {
        var category = await GetCategoryEntityAsync(input.CategoryId);
        var entry = _manager.CreateEntry(
            input.EntryDate,
            input.Direction,
            category,
            input.Amount,
            input.PaymentStatus,
            input.DueDate,
            input.PaymentDate,
            input.Counterparty,
            input.ReferenceNo,
            input.Description,
            input.Note);

        await _entryRepository.InsertAsync(entry, autoSave: true);
        return ToDto(entry, await GetCategoryCodeMapAsync([entry.CategoryId]));
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.ManageEntries)]
    public async Task<OperatingCostEntryDto> UpdateEntryAsync(Guid id, UpdateOperatingCostEntryDto input)
    {
        var entry = await GetEntryEntityAsync(id);
        var category = await GetCategoryEntityAsync(input.CategoryId);
        _manager.EnsureCategoryCanBeUsed(category, input.Direction);

        entry.UpdateInfo(
            input.EntryDate,
            input.Direction,
            category.Id,
            category.Name,
            input.Amount,
            input.PaymentStatus,
            input.DueDate,
            input.PaymentDate,
            input.Counterparty,
            input.ReferenceNo,
            input.Description,
            input.Note);

        await _entryRepository.UpdateAsync(entry, autoSave: true);
        return ToDto(entry, await GetCategoryCodeMapAsync([entry.CategoryId]));
    }

    [Authorize(VPureLuxPermissions.OperatingCosts.Delete)]
    public async Task DeleteEntryAsync(Guid id)
    {
        var entry = await GetEntryEntityAsync(id);
        await _entryRepository.DeleteAsync(entry, autoSave: true);
    }

    private async Task<OperatingCostCategory> GetCategoryEntityAsync(Guid id)
    {
        var category = await _categoryRepository.FindAsync(id);
        if (category == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostCategoryNotFound)
                .WithData(nameof(id), id);
        }

        return category;
    }

    private async Task<OperatingCostEntry> GetEntryEntityAsync(Guid id)
    {
        var entry = await _entryRepository.FindAsync(id);
        if (entry == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostEntryNotFound)
                .WithData(nameof(id), id);
        }

        return entry;
    }

    private async Task<Dictionary<Guid, string>> GetCategoryCodeMapAsync(IEnumerable<Guid> categoryIds)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var categories = await AsyncExecuter.ToListAsync((await _categoryRepository.GetQueryableAsync())
            .Where(x => ids.Contains(x.Id))
        );

        return categories.ToDictionary(x => x.Id, x => x.Code);
    }

    private static IQueryable<OperatingCostCategory> ApplyCategoryFilter(
        IQueryable<OperatingCostCategory> query,
        GetOperatingCostCategoryListInput input)
    {
        if (!input.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.Code.Contains(input.SearchText!) ||
                x.Name.Contains(input.SearchText!));
        }

        if (input.Direction.HasValue)
        {
            query = query.Where(x => x.Direction == input.Direction.Value);
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        return query;
    }

    private static IQueryable<OperatingCostEntry> ApplyEntryFilter(
        IQueryable<OperatingCostEntry> query,
        GetOperatingCostEntryListInput input)
    {
        if (!input.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.CategoryNameSnapshot.Contains(input.SearchText!) ||
                x.Description.Contains(input.SearchText!) ||
                (x.Counterparty != null && x.Counterparty.Contains(input.SearchText!)) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(input.SearchText!)));
        }

        if (input.Direction.HasValue)
        {
            query = query.Where(x => x.Direction == input.Direction.Value);
        }

        if (input.PaymentStatus.HasValue)
        {
            query = query.Where(x => x.PaymentStatus == input.PaymentStatus.Value);
        }

        if (input.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == input.CategoryId.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(x => x.EntryDate >= input.FromDate.Value.Date);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(x => x.EntryDate <= input.ToDate.Value.Date);
        }

        return query;
    }

    private static IQueryable<OperatingCostCategory> ApplyCategorySorting(
        IQueryable<OperatingCostCategory> query,
        string? sorting)
    {
        if (sorting.IsNullOrWhiteSpace())
        {
            return query.OrderBy(x => x.Direction).ThenBy(x => x.Name);
        }

        var (field, desc) = ParseSorting(sorting);
        return field switch
        {
            "code" => desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "direction" => desc ? query.OrderByDescending(x => x.Direction) : query.OrderBy(x => x.Direction),
            "isactive" => desc ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            _ => query.OrderBy(x => x.Direction).ThenBy(x => x.Name)
        };
    }

    private static IQueryable<OperatingCostEntry> ApplyEntrySorting(
        IQueryable<OperatingCostEntry> query,
        string? sorting)
    {
        if (sorting.IsNullOrWhiteSpace())
        {
            return query.OrderByDescending(x => x.EntryDate).ThenByDescending(x => x.CreationTime);
        }

        var (field, desc) = ParseSorting(sorting);
        return field switch
        {
            "entrydate" => desc ? query.OrderByDescending(x => x.EntryDate) : query.OrderBy(x => x.EntryDate),
            "direction" => desc ? query.OrderByDescending(x => x.Direction) : query.OrderBy(x => x.Direction),
            "amount" => desc ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            "paymentstatus" => desc ? query.OrderByDescending(x => x.PaymentStatus) : query.OrderBy(x => x.PaymentStatus),
            "counterparty" => desc ? query.OrderByDescending(x => x.Counterparty) : query.OrderBy(x => x.Counterparty),
            _ => query.OrderByDescending(x => x.EntryDate).ThenByDescending(x => x.CreationTime)
        };
    }

    private static (string Field, bool Desc) ParseSorting(string sorting)
    {
        var parts = sorting.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var field = parts[0].Split('.').Last().ToLowerInvariant();
        var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (field, desc);
    }

    private static OperatingCostCategoryDto ToDto(OperatingCostCategory category) => new()
    {
        Id = category.Id,
        Code = category.Code,
        Name = category.Name,
        Direction = category.Direction,
        IsActive = category.IsActive,
        CreationTime = category.CreationTime
    };

    private static OperatingCostEntryDto ToDto(OperatingCostEntry entry, IReadOnlyDictionary<Guid, string> categoryCodeMap) => new()
    {
        Id = entry.Id,
        EntryDate = entry.EntryDate,
        Direction = entry.Direction,
        CategoryId = entry.CategoryId,
        CategoryCode = categoryCodeMap.TryGetValue(entry.CategoryId, out var code) ? code : string.Empty,
        CategoryName = entry.CategoryNameSnapshot,
        Amount = entry.Amount,
        PaymentStatus = entry.PaymentStatus,
        DueDate = entry.DueDate,
        PaymentDate = entry.PaymentDate,
        Counterparty = entry.Counterparty,
        ReferenceNo = entry.ReferenceNo,
        Description = entry.Description,
        Note = entry.Note,
        CreationTime = entry.CreationTime
    };

}
