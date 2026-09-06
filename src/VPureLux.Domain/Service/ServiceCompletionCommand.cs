using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Volo.Abp;

namespace VPureLux.Service;

public sealed record ServiceCompletionLine(Guid LineId, int ActualQuantity);

public sealed class ServiceCompletionCommand
{
    public Guid OrderId { get; }
    public string Key { get; }
    public DateTime CompletedAt { get; }
    public IReadOnlyList<ServiceCompletionLine> Lines { get; }
    public string Hash { get; }

    public ServiceCompletionCommand(Guid orderId, string key, DateTimeOffset completedAt,
        IReadOnlyCollection<ServiceCompletionLine> lines)
    {
        if (orderId == Guid.Empty || string.IsNullOrWhiteSpace(key) || key.Length > 64 ||
            completedAt == default || lines == null || lines.Count == 0 ||
            lines.Any(x => x is null || x.LineId == Guid.Empty || x.ActualQuantity < 0) ||
            lines.Select(x => x.LineId).Distinct().Count() != lines.Count)
        {
            throw new BusinessException(ServiceErrorCodes.InvalidCompletion);
        }

        OrderId = orderId;
        Key = key;
        CompletedAt = completedAt.UtcDateTime;
        Lines = lines.OrderBy(x => x.LineId).ToList().AsReadOnly();
        // Versioned, culture-independent facts. Expected version is not a business fact on replay.
        var canonical = new StringBuilder("service-completion-v1|").Append(orderId.ToString("N"))
            .Append('|').Append(CompletedAt.Ticks.ToString(CultureInfo.InvariantCulture));
        foreach (var line in Lines)
        {
            canonical.Append('|').Append(line.LineId.ToString("N")).Append(':')
                .Append(line.ActualQuantity.ToString(CultureInfo.InvariantCulture));
        }
        Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
