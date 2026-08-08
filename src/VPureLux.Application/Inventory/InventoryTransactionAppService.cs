using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.BusinessCodes;
using VPureLux.Permissions;
using VPureLux.Suppliers;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace VPureLux.Inventory;

[Authorize(VPureLuxPermissions.Inventory.View)]
public class InventoryTransactionAppService : ApplicationService, IInventoryTransactionAppService
{
    private const string InventoryLotSequenceName = "InventoryLot";
    private const string InventoryLotPrefix = "LOT";

    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly IInventoryLotSupplierRepository _lotSupplierRepository;
    private readonly IInventoryBalanceRepository _balanceRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly InventoryManager _manager;
    private readonly InventoryApplicationMapper _mapper;
    private readonly IBusinessCodeGenerator _businessCodeGenerator;

    public InventoryTransactionAppService(
        IInventoryTransactionRepository transactionRepository,
        IInventoryLotRepository lotRepository,
        IInventoryLotSupplierRepository lotSupplierRepository,
        IInventoryBalanceRepository balanceRepository,
        ISupplierRepository supplierRepository,
        InventoryManager manager,
        InventoryApplicationMapper mapper,
        IBusinessCodeGenerator businessCodeGenerator)
    {
        _transactionRepository = transactionRepository;
        _lotRepository = lotRepository;
        _lotSupplierRepository = lotSupplierRepository;
        _balanceRepository = balanceRepository;
        _supplierRepository = supplierRepository;
        _manager = manager;
        _mapper = mapper;
        _businessCodeGenerator = businessCodeGenerator;
    }

    public async Task<InventoryTransactionDto> GetAsync(Guid id)
    {
        var transaction = await _transactionRepository.FindAsync(id, includeDetails: true)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.InventoryTransactionNotFound);
        return _mapper.ToDto(transaction);
    }

    [Authorize(VPureLuxPermissions.Inventory.Receive)]
    public async Task<InventoryTransactionDto> PostReceiptAsync(PostReceiptDto input)
    {
        ValidateReceiptInput(input);
        var hash = HashReceipt(input);
        var existing = await GetIdempotentResultAsync(input.IdempotencyKey, hash);
        if (existing != null)
        {
            return _mapper.ToDto(existing);
        }

        var supplier = input.SupplierId.HasValue
            ? await GetSupplierAsync(input.SupplierId.Value)
            : null;
        var receiptLines = await ResolveReceiptLotNumbersAsync(input.Lines);

        var transaction = _manager.CreateTransaction(
            input.WarehouseId,
            InventoryTransactionType.PurchaseReceipt,
            input.IdempotencyKey,
            hash,
            input.ReferenceType,
            input.ReferenceId,
            input.BomVersionId);
        var lots = new List<InventoryLot>();

        foreach (var inputLine in receiptLines)
        {
            await _manager.EnsureWarehouseAndStockItemUsableAsync(input.WarehouseId, inputLine.StockItemId);
            var line = transaction.AddReceiptLine(
                GuidGenerator.Create(),
                inputLine.StockItemId,
                inputLine.Quantity,
                inputLine.LotNo!,
                inputLine.ReceivedAt,
                inputLine.UnitCost);
            lots.Add(_manager.CreateLot(input.WarehouseId, line));
        }

        transaction.Post(Clock.Now);
        await _transactionRepository.InsertAsync(transaction);
        foreach (var lot in lots)
        {
            await _lotRepository.InsertAsync(lot);
            if (supplier != null)
            {
                await _lotSupplierRepository.InsertAsync(_manager.CreateLotSupplier(lot, supplier));
            }

            await _balanceRepository.ApplyMovementAsync(
                lot.WarehouseId,
                lot.StockItemId,
                lot.ReceivedQuantity,
                lot.ReceivedQuantity * lot.UnitCost,
                transaction.PostedAt!.Value);
        }

        await _transactionRepository.UpdateAsync(transaction, autoSave: true);
        return _mapper.ToDto(transaction);
    }

    [Authorize(VPureLuxPermissions.Inventory.Issue)]
    public async Task<IssueCostResultDto> PostIssueAsync(PostIssueDto input)
    {
        if (input.Type is not (InventoryTransactionType.SalesIssue or InventoryTransactionType.AssemblyIssue))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var hash = HashIssue(input);
        var existing = await GetIdempotentResultAsync(input.IdempotencyKey, hash);
        if (existing != null)
        {
            return ToIssueResult(existing);
        }

        var transaction = _manager.CreateTransaction(
            input.WarehouseId,
            input.Type,
            input.IdempotencyKey,
            hash,
            input.ReferenceType,
            input.ReferenceId,
            input.BomVersionId);

        foreach (var inputLine in ConsolidateIssueLines(input.Lines))
        {
            await _manager.EnsureWarehouseAndStockItemUsableAsync(input.WarehouseId, inputLine.StockItemId);
            var line = transaction.AddIssueLine(GuidGenerator.Create(), inputLine.StockItemId, inputLine.Quantity);
            var allocations = await _manager.AllocateFifoAsync(transaction, line);
            foreach (var allocation in allocations)
            {
                var lot = await _lotRepository.GetAsync(allocation.InventoryLotId);
                await _lotRepository.UpdateAsync(lot);
            }

            await _balanceRepository.ApplyMovementAsync(
                input.WarehouseId,
                inputLine.StockItemId,
                -inputLine.Quantity,
                -allocations.Sum(x => x.TotalCost),
                Clock.Now);
        }

        transaction.Post(Clock.Now);
        await _transactionRepository.InsertAsync(transaction, autoSave: true);
        return ToIssueResult(transaction);
    }

    [Authorize(VPureLuxPermissions.Inventory.Adjust)]
    public async Task<InventoryTransactionDto> PostAdjustmentAsync(PostAdjustmentDto input)
    {
        if (input.Type == InventoryTransactionType.AdjustmentIncrease)
        {
            return await PostAdjustmentIncreaseAsync(input);
        }

        if (input.Type == InventoryTransactionType.AdjustmentDecrease)
        {
            return await PostAdjustmentDecreaseAsync(input);
        }

        throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    private async Task<InventoryTransactionDto> PostAdjustmentIncreaseAsync(PostAdjustmentDto input)
    {
        var hash = HashAdjustment(input);
        var existing = await GetIdempotentResultAsync(input.IdempotencyKey, hash);
        if (existing != null)
        {
            return _mapper.ToDto(existing);
        }

        var increaseLines = await ResolveReceiptLotNumbersAsync(input.IncreaseLines);

        var transaction = _manager.CreateTransaction(
            input.WarehouseId,
            InventoryTransactionType.AdjustmentIncrease,
            input.IdempotencyKey,
            hash,
            input.ReferenceType,
            input.ReferenceId,
            input.BomVersionId,
            input.Reason);
        var lots = new List<InventoryLot>();

        foreach (var inputLine in increaseLines)
        {
            await _manager.EnsureWarehouseAndStockItemUsableAsync(input.WarehouseId, inputLine.StockItemId);
            var line = transaction.AddReceiptLine(
                GuidGenerator.Create(), inputLine.StockItemId, inputLine.Quantity,
                inputLine.LotNo!, inputLine.ReceivedAt, inputLine.UnitCost);
            lots.Add(_manager.CreateLot(input.WarehouseId, line));
        }

        transaction.Post(Clock.Now);
        await _transactionRepository.InsertAsync(transaction);
        foreach (var lot in lots)
        {
            await _lotRepository.InsertAsync(lot);
            await _balanceRepository.ApplyMovementAsync(
                lot.WarehouseId, lot.StockItemId, lot.ReceivedQuantity,
                lot.ReceivedQuantity * lot.UnitCost, transaction.PostedAt!.Value);
        }
        await _transactionRepository.UpdateAsync(transaction, autoSave: true);
        return _mapper.ToDto(transaction);
    }

    private async Task<InventoryTransactionDto> PostAdjustmentDecreaseAsync(PostAdjustmentDto input)
    {
        var hash = HashAdjustment(input);
        var existing = await GetIdempotentResultAsync(input.IdempotencyKey, hash);
        if (existing != null)
        {
            return _mapper.ToDto(existing);
        }

        var transaction = _manager.CreateTransaction(
            input.WarehouseId,
            InventoryTransactionType.AdjustmentDecrease,
            input.IdempotencyKey,
            hash,
            input.ReferenceType,
            input.ReferenceId,
            input.BomVersionId,
            input.Reason);

        foreach (var inputLine in ConsolidateIssueLines(input.DecreaseLines))
        {
            await _manager.EnsureWarehouseAndStockItemUsableAsync(input.WarehouseId, inputLine.StockItemId);
            var line = transaction.AddIssueLine(GuidGenerator.Create(), inputLine.StockItemId, inputLine.Quantity);
            var allocations = await _manager.AllocateFifoAsync(transaction, line);
            foreach (var allocation in allocations)
            {
                var lot = await _lotRepository.GetAsync(allocation.InventoryLotId);
                await _lotRepository.UpdateAsync(lot);
            }

            await _balanceRepository.ApplyMovementAsync(
                input.WarehouseId,
                inputLine.StockItemId,
                -inputLine.Quantity,
                -allocations.Sum(x => x.TotalCost),
                Clock.Now);
        }

        transaction.Post(Clock.Now);
        await _transactionRepository.InsertAsync(transaction, autoSave: true);
        return _mapper.ToDto(transaction);
    }

    private async Task<InventoryTransaction?> GetIdempotentResultAsync(string key, string requestHash)
    {
        var existing = await _manager.FindExistingTransactionAsync(key);
        if (existing != null && existing.RequestHash != requestHash)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict);
        }

        return existing;
    }

    private async Task<Supplier> GetSupplierAsync(Guid supplierId)
    {
        return await _supplierRepository.FindAsync(supplierId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.SupplierNotFound)
                .WithData(nameof(supplierId), supplierId);
    }

    private IssueCostResultDto ToIssueResult(InventoryTransaction transaction)
    {
        var allocations = transaction.Lines.SelectMany(x => x.Allocations).Select(_mapper.ToDto).ToList();
        var totalQuantity = transaction.Lines.Sum(x => x.Quantity);
        return new IssueCostResultDto
        {
            InventoryTransactionId = transaction.Id,
            TotalIssueCost = transaction.TotalIssueCost,
            WeightedUnitCost = totalQuantity == 0 ? 0 : transaction.TotalIssueCost / totalQuantity,
            Allocations = allocations
        };
    }

    private static List<IssueLineInput> ConsolidateIssueLines(IEnumerable<IssueLineInput> lines) =>
        lines.GroupBy(x => x.StockItemId)
            .Select(x => new IssueLineInput { StockItemId = x.Key, Quantity = x.Sum(y => y.Quantity) })
            .ToList();

    private async Task<List<ReceiptLineInput>> ResolveReceiptLotNumbersAsync(IEnumerable<ReceiptLineInput> lines)
    {
        var result = new List<ReceiptLineInput>();

        foreach (var line in lines)
        {
            result.Add(new ReceiptLineInput
            {
                StockItemId = line.StockItemId,
                Quantity = line.Quantity,
                LotNo = string.IsNullOrWhiteSpace(line.LotNo)
                    ? await GenerateLotNoAsync()
                    : line.LotNo.Trim(),
                ReceivedAt = line.ReceivedAt,
                UnitCost = line.UnitCost
            });
        }

        return result;
    }

    private static void ValidateReceiptInput(PostReceiptDto input)
    {
        if (input.Lines.Count == 0 || input.Lines.Any(x => x.Quantity <= 0))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
    }

    private async Task<string> GenerateLotNoAsync()
    {
        var businessDate = Clock.Now.Date;
        var datePart = businessDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var lotNoPrefix = $"{InventoryLotPrefix}-{datePart}";

        return await _businessCodeGenerator.GenerateAsync(new BusinessCodeGenerationContext
        {
            SequenceName = InventoryLotSequenceName,
            Prefix = InventoryLotPrefix,
            Date = businessDate,
            ExistsAsync = (candidate, cancellationToken) =>
                _lotRepository.LotNoExistsAsync(candidate, cancellationToken),
            SeedMaxAsync = async cancellationToken =>
                (int?)await _lotRepository.GetMaxLotNoSequenceAsync(lotNoPrefix, cancellationToken)
        });
    }

    private static string HashReceipt(PostReceiptDto input) =>
        Hash($"{input.WarehouseId}|{input.SupplierId}|{input.ReferenceType}|{input.ReferenceId}|{input.BomVersionId}|" +
             string.Join(";", input.Lines.OrderBy(x => x.StockItemId).ThenBy(x => x.LotNo)
                 .Select(x => $"{x.StockItemId}:{x.Quantity}:{x.LotNo}:{x.ReceivedAt:O}:{x.UnitCost}")));

    private static string HashIssue(PostIssueDto input) =>
        Hash($"{input.WarehouseId}|{input.Type}|{input.ReferenceType}|{input.ReferenceId}|{input.BomVersionId}|" +
             string.Join(";", ConsolidateIssueLines(input.Lines).OrderBy(x => x.StockItemId)
                 .Select(x => $"{x.StockItemId}:{x.Quantity}")));

    private static string HashAdjustment(PostAdjustmentDto input) =>
        Hash($"{input.WarehouseId}|{input.Type}|{input.ReferenceType}|{input.ReferenceId}|{input.BomVersionId}|{input.Reason}|" +
             string.Join(";", input.IncreaseLines.OrderBy(x => x.StockItemId).ThenBy(x => x.LotNo)
                 .Select(x => $"{x.StockItemId}:{x.Quantity}:{x.LotNo}:{x.ReceivedAt:O}:{x.UnitCost}")) + "|" +
             string.Join(";", ConsolidateIssueLines(input.DecreaseLines).OrderBy(x => x.StockItemId)
                 .Select(x => $"{x.StockItemId}:{x.Quantity}")));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
