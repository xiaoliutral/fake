namespace Fake.Domain.Entities.Auditing;

public interface IHasCreateUserId: IHasCreateUserId<Guid>;

public interface IHasCreateUserId<out TUser>
{
    /// <summary>
    /// 创建用户Id
    /// </summary>
    TUser CreateUserId { get; }
}