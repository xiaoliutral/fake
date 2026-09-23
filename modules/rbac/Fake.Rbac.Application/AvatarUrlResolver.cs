using Fake.ObjectStorage;
using Microsoft.Extensions.Options;

namespace Fake.Rbac.Application;

/// <summary>
/// 用户头像字段：库中存 FileId（或历史 URL），对外返回可访问 URL。
/// </summary>
public class AvatarUrlResolver(IObjectStorage objectStorage, IOptions<FakeObjectStorageOptions> options)
{
    public async Task<string?> ResolveAsync(string? avatar, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(avatar))
        {
            return null;
        }

        avatar = avatar.Trim();

        // 历史数据：完整 URL 或本地相对路径
        if (avatar.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            avatar.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            avatar.StartsWith('/'))
        {
            return avatar;
        }

        if (!Guid.TryParse(avatar, out _))
        {
            return avatar;
        }

        TimeSpan? expires = null;
        if (options.Value.SignUrlsByDefault)
        {
            expires = TimeSpan.FromSeconds(options.Value.DefaultSignedUrlExpiresSeconds);
        }

        return await objectStorage.GetUrlAsync(avatar, expires, cancellationToken);
    }
}
