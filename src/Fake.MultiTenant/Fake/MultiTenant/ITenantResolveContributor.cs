﻿namespace Fake.MultiTenant;

public interface ITenantResolveContributor
{
    string Name { get; }

    Task ResolveAsync(TenantResolveContext context);
}