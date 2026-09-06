using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VPureLux.Web.Pages.Service;

public class ServiceOrderForm
{
    public Guid CustomerAssetId { get; set; }
    public Guid WarehouseId { get; set; }
    [DataType(DataType.Date)] public DateTime OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? TechnicianUserId { get; set; }
    [StringLength(VPureLux.Service.ServiceConsts.MaxAddressLength)] public string? ServiceAddress { get; set; }
    [StringLength(VPureLux.Service.ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
    [Required, MinLength(1)] public List<ServiceOrderLineForm> Lines { get; set; } = [];
    [StringLength(40)] public string? ConcurrencyStamp { get; set; }
    public string? AssetLabel { get; set; }
    public string? TechnicianLabel { get; set; }

    public VPureLux.Service.CreateServiceOrderDto ToCreateInput() => new()
    {
        CustomerAssetId = CustomerAssetId,
        WarehouseId = WarehouseId,
        OrderDate = OrderDate,
        ScheduledAt = ScheduledAt,
        TechnicianUserId = TechnicianUserId,
        ServiceAddress = ServiceAddress,
        Note = Note,
        Lines = Lines.ConvertAll(line => line.ToInput())
    };

    public VPureLux.Service.UpdateServiceOrderDto ToUpdateInput() => new()
    {
        OrderDate = OrderDate,
        ScheduledAt = ScheduledAt,
        TechnicianUserId = TechnicianUserId,
        ServiceAddress = ServiceAddress,
        Note = Note,
        ConcurrencyStamp = ConcurrencyStamp ?? string.Empty,
        Lines = Lines.ConvertAll(line => line.ToInput())
    };
}

public class ServiceOrderLineForm
{
    public Guid? Id { get; set; }
    [EnumDataType(typeof(VPureLux.Service.ServiceOrderLineType))]
    public VPureLux.Service.ServiceOrderLineType LineType { get; set; }
    public Guid CatalogItemId { get; set; }
    public Guid? CustomerAssetComponentId { get; set; }
    [Range(1, 100000)] public int Quantity { get; set; } = 1;
    [ModelBinder(BinderType = typeof(ServiceMoneyModelBinder))]
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }
    [StringLength(VPureLux.Service.ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
    public string? ItemLabel { get; set; }
    public string? PositionLabel { get; set; }

    public VPureLux.Service.ServiceOrderLineInput ToInput() => new()
    {
        Id = Id,
        LineType = LineType,
        CatalogItemId = CatalogItemId,
        CustomerAssetComponentId = CustomerAssetComponentId,
        Quantity = Quantity,
        UnitPrice = UnitPrice,
        Note = Note
    };
}

public class ServiceOrderPageViewModel
{
    public ServiceOrderForm Input { get; init; } = new();
    public bool IsEdit { get; init; }
    public string? OrderNo { get; init; }
    public Guid? OrderId { get; init; }
    public List<SelectListItem> Warehouses { get; init; } = [];
}
