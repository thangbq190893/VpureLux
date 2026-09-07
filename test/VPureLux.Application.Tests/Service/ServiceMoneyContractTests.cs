using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Shouldly;
using VPureLux.Sales;
using Xunit;

namespace VPureLux.Service;

public class ServiceMoneyContractTests
{
    [Theory]
    [InlineData(null, false)] [InlineData("0", false)] [InlineData("-1", false)] [InlineData("1500000.50", true)]
    public void Money_inputs_require_positive_amount_in_vi_culture(string? amount, bool valid)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            decimal? number = amount == null ? null : decimal.Parse(amount, CultureInfo.InvariantCulture);
            var payment = new AddServicePaymentDto { Amount = number, PaymentDate = DateTimeOffset.UtcNow,
                PaymentMethod = SalesPaymentMethod.Cash, IdempotencyKey = "key" };
            Validator.TryValidateObject(payment, new ValidationContext(payment), new List<ValidationResult>(), true).ShouldBe(valid);
            var refund = new AddServiceRefundDto { Amount = number, RefundDate = DateTimeOffset.UtcNow,
                Method = SalesPaymentMethod.Cash, IdempotencyKey = "key", Reason = "actual return" };
            Validator.TryValidateObject(refund, new ValidationContext(refund), new List<ValidationResult>(), true).ShouldBe(valid);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Refund_contract_has_no_edit_void_delete_and_history_is_paged()
    {
        var methods = typeof(IServicePaymentAppService).GetMethods().Select(x => x.Name).ToArray();
        methods.ShouldNotContain("DeleteAsync"); methods.ShouldNotContain("UpdateAsync"); methods.ShouldNotContain("VoidRefundAsync");
        typeof(ServiceMoneyHistoryInput).IsSubclassOf(typeof(Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto)).ShouldBeTrue();
        new ServiceMoneySummaryDto().ActualTotal.ShouldBeNull();
    }
}
