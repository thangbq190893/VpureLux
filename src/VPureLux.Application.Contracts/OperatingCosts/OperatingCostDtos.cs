using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace VPureLux.OperatingCosts;

public class OperatingCostCategoryDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OperatingCostDirection Direction { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }
}

public class OperatingCostEntryDto : EntityDto<Guid>
{
    public DateTime EntryDate { get; set; }
    public OperatingCostDirection Direction { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public OperatingCostPaymentStatus PaymentStatus { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Counterparty { get; set; }
    public string? ReferenceNo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreationTime { get; set; }
}

public class OperatingCostSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetAmount { get; set; }
    public decimal UnpaidReceivable { get; set; }
    public decimal UnpaidPayable { get; set; }
}

public class UpdateOperatingCostCategoryDto
{
    [Required, StringLength(OperatingCostConsts.MaxCategoryCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(OperatingCostConsts.MaxCategoryNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public OperatingCostDirection Direction { get; set; } = OperatingCostDirection.Expense;

    public bool IsActive { get; set; } = true;
}

public class CreateOperatingCostCategoryDto : UpdateOperatingCostCategoryDto
{
}

public class UpdateOperatingCostEntryDto
{
    [Required]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    [Required]
    public OperatingCostDirection Direction { get; set; } = OperatingCostDirection.Expense;

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public OperatingCostPaymentStatus PaymentStatus { get; set; } = OperatingCostPaymentStatus.Paid;

    public DateTime? DueDate { get; set; }

    public DateTime? PaymentDate { get; set; }

    [StringLength(OperatingCostConsts.MaxCounterpartyLength)]
    public string? Counterparty { get; set; }

    [StringLength(OperatingCostConsts.MaxReferenceNoLength)]
    public string? ReferenceNo { get; set; }

    [Required, StringLength(OperatingCostConsts.MaxDescriptionLength)]
    public string Description { get; set; } = string.Empty;

    [StringLength(OperatingCostConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class CreateOperatingCostEntryDto : UpdateOperatingCostEntryDto
{
}

public class GetOperatingCostCategoryListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public OperatingCostDirection? Direction { get; set; }
    public bool? IsActive { get; set; }
}

public class GetOperatingCostEntryListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public OperatingCostDirection? Direction { get; set; }
    public OperatingCostPaymentStatus? PaymentStatus { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
