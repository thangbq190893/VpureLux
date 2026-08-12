using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.OperatingCosts;

public class OperatingCostManager : DomainService
{
    private readonly IRepository<OperatingCostCategory, Guid> _categoryRepository;
    private readonly IRepository<OperatingCostEntry, Guid> _entryRepository;
    private readonly IGuidGenerator _guidGenerator;

    public OperatingCostManager(
        IRepository<OperatingCostCategory, Guid> categoryRepository,
        IRepository<OperatingCostEntry, Guid> entryRepository,
        IGuidGenerator guidGenerator)
    {
        _categoryRepository = categoryRepository;
        _entryRepository = entryRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<OperatingCostCategory> CreateCategoryAsync(
        string code,
        string name,
        OperatingCostDirection direction)
    {
        code = OperatingCostCategory.NormalizeCode(code);
        if (await _categoryRepository.AnyAsync(x => x.Code == code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostCategoryCodeAlreadyExists)
                .WithData(nameof(code), code);
        }

        return new OperatingCostCategory(_guidGenerator.Create(), code, name, direction);
    }

    public async Task EnsureCategoryCodeCanBeUsedAsync(Guid id, string code)
    {
        code = OperatingCostCategory.NormalizeCode(code);
        if (await _categoryRepository.AnyAsync(x => x.Code == code && x.Id != id))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostCategoryCodeAlreadyExists)
                .WithData(nameof(code), code);
        }
    }

    public async Task EnsureCanDeleteCategoryAsync(Guid categoryId)
    {
        if (await _entryRepository.AnyAsync(x => x.CategoryId == categoryId))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostCategoryInUse)
                .WithData(nameof(categoryId), categoryId);
        }
    }

    public OperatingCostEntry CreateEntry(
        DateTime entryDate,
        OperatingCostDirection direction,
        OperatingCostCategory category,
        decimal amount,
        OperatingCostPaymentStatus paymentStatus,
        DateTime? dueDate,
        DateTime? paymentDate,
        string? counterparty,
        string? referenceNo,
        string description,
        string? note)
    {
        EnsureCategoryCanBeUsed(category, direction);

        return new OperatingCostEntry(
            _guidGenerator.Create(),
            entryDate,
            direction,
            category.Id,
            category.Name,
            amount,
            paymentStatus,
            dueDate,
            paymentDate,
            counterparty,
            referenceNo,
            description,
            note);
    }

    public void EnsureCategoryCanBeUsed(OperatingCostCategory category, OperatingCostDirection direction)
    {
        Check.NotNull(category, nameof(category));

        if (!category.IsActive)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostCategoryInactive)
                .WithData(nameof(category.Id), category.Id);
        }

        if (category.Direction != direction)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostCategoryDirectionMismatch)
                .WithData(nameof(category.Id), category.Id);
        }
    }
}
