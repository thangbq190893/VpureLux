using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPureLux.CustomerCare;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace VPureLux.Warranty;

public class CustomerCareSalesIntakeWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    public CustomerCareSalesIntakeWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CustomerCareOptions> options)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = Math.Clamp(
            options.Value.SalesIntakeWorkerPeriodMilliseconds,
            10_000,
            3_600_000);
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await workerContext.ServiceProvider
            .GetRequiredService<CustomerCareSalesIntakeService>()
            .RunBatchAsync(cancellationToken: workerContext.CancellationToken);
    }
}
