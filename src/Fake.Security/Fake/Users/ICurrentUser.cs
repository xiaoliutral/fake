﻿using System.Security.Claims;

namespace Fake.Users;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>
    /// 用户唯一标识
    /// </summary>
    Guid? Id { get; }

    /// <summary>
    /// 租户id
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// 获取当前用户的名称。
    /// </summary>
    string? UserName { get; }

    string? Email { get; }
    string[] Roles { get; }

    Claim? FindClaimOrNull(string claimType);

    Claim[] FindClaims(string claimType);
}