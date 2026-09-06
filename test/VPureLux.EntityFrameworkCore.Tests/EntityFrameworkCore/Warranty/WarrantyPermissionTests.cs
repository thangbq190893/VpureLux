using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Warranty;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class WarrantyPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_define_warranty_configuration_permissions()
    {
        var definitions = GetRequiredService<IPermissionDefinitionManager>();

        (await definitions.GetAsync(VPureLuxPermissions.Warranty.View)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManagePolicies)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageMachines)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageSyncFailures)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageInstallations)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageAssets)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageReminders)).ShouldNotBeNull();
    }

    [Fact]
    public void Should_protect_warranty_configuration_commands()
    {
        Permission(nameof(WarrantyAppService.SetPolicyAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManagePolicies);
        Permission(nameof(WarrantyAppService.SetMachineSettingAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageMachines);
        Permission(nameof(WarrantyAppService.GetMachineSettingsByProductIdsAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageMachines);
        Permission(nameof(WarrantyAppService.GetPoliciesByComponentIdsAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManagePolicies);
        Permission(nameof(WarrantyAppService.RetrySyncFailureAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageSyncFailures);
        Permission(nameof(WarrantyAppService.ConfirmInstallationAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageInstallations);
        Permission(nameof(WarrantyAppService.CreateExternalAssetAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageAssets);
        Permission(nameof(WarrantyAppService.SuspendAssetAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageReminders);
    }

    [Fact]
    public async Task Each_warranty_permission_should_deny_and_allow_through_server_authorization()
    {
        var authorization = GetRequiredService<IAuthorizationService>();
        authorization.ShouldBeOfType<WarrantyMatrixAuthorizationService>();
        var warranty = GetRequiredService<IWarrantyAppService>();
        var cases = new (string Permission, Func<Task> Action, bool AllowedEndsWithNotFound)[]
        {
            (VPureLuxPermissions.Warranty.View,
                async () => await warranty.GetNotificationSummaryAsync(), false),
            (VPureLuxPermissions.Warranty.ManageMachines,
                async () => await warranty.GetMachineSettingsByProductIdsAsync([]), false),
            (VPureLuxPermissions.Warranty.ManagePolicies,
                async () => await warranty.GetPoliciesByComponentIdsAsync([]), false),
            (VPureLuxPermissions.Warranty.ManageInstallations,
                async () => await warranty.GetPendingInstallationListAsync(new GetPendingInstallationListInput()), false),
            (VPureLuxPermissions.Warranty.ManageAssets,
                async () => await warranty.GetAssetListAsync(new GetCustomerAssetListInput()), false),
            (VPureLuxPermissions.Warranty.ManageReminders,
                async () => await warranty.CompleteReminderAsync(Guid.NewGuid(), new CompleteReplacementReminderDto
                {
                    Note = "Authorization matrix",
                    IdempotencyKey = Guid.NewGuid().ToString("N")
                }), true),
            (VPureLuxPermissions.Warranty.ManageSyncFailures,
                async () => await warranty.GetSyncFailureListAsync(new GetCustomerCareSyncFailureListInput()), false)
        };

        foreach (var item in cases)
        {
            using (WarrantyMatrixAuthorizationService.Deny(item.Permission))
            {
                await Should.ThrowAsync<AbpAuthorizationException>(() => authorization.CheckAsync(item.Permission));
                await Should.ThrowAsync<AbpAuthorizationException>(item.Action);
            }

            await authorization.CheckAsync(item.Permission);

            if (item.AllowedEndsWithNotFound)
            {
                var exception = await Should.ThrowAsync<BusinessException>(item.Action);
                exception.Code.ShouldBe(VPureLuxDomainErrorCodes.EntityNotFound);
            }
            else
            {
                await item.Action();
            }
        }
    }

    private static string? Permission(string methodName) =>
        typeof(WarrantyAppService).GetMethods()
            .Single(method => method.Name == methodName)
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy;
}

[Dependency(ReplaceServices = true)]
public class WarrantyMatrixAuthorizationService : IAbpAuthorizationService, ITransientDependency
{
    private static readonly AsyncLocal<string?> DeniedPermission = new();
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public IServiceProvider ServiceProvider { get; }
    public ClaimsPrincipal CurrentPrincipal => _principalAccessor.Principal;
    public static string? CurrentDeniedPermission => DeniedPermission.Value;

    public WarrantyMatrixAuthorizationService(
        IServiceProvider serviceProvider,
        ICurrentPrincipalAccessor principalAccessor)
    {
        ServiceProvider = serviceProvider;
        _principalAccessor = principalAccessor;
    }

    public static IDisposable Deny(string permission)
    {
        var previous = DeniedPermission.Value;
        DeniedPermission.Value = permission;
        return new DisposeAction(() => DeniedPermission.Value = previous);
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        return Task.FromResult(DeniedPermission.Value == null
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed());
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        string policyName) =>
        Task.FromResult(policyName == DeniedPermission.Value
            ? AuthorizationResult.Failed()
            : AuthorizationResult.Success());

    private sealed class DisposeAction : IDisposable
    {
        private readonly Action _dispose;

        public DisposeAction(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose() => _dispose();
    }
}

public class WarrantyMatrixMethodInvocationAuthorizationService : IMethodInvocationAuthorizationService, ITransientDependency
{
    public Task CheckAsync(MethodInvocationAuthorizationContext context)
    {
        var denied = WarrantyMatrixAuthorizationService.CurrentDeniedPermission;
        if (denied == null)
        {
            return Task.CompletedTask;
        }

        var methodPolicies = context.Method.GetCustomAttributes<AuthorizeAttribute>(true)
            .Select(attribute => attribute.Policy);
        var classPolicies = context.Method.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(true)
            .Select(attribute => attribute.Policy) ?? [];
        if (methodPolicies.Concat(classPolicies).Contains(denied, StringComparer.Ordinal))
        {
            throw new AbpAuthorizationException();
        }

        return Task.CompletedTask;
    }
}
