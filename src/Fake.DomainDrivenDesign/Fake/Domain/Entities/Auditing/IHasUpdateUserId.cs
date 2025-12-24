namespace Fake.Domain.Entities.Auditing;

public interface IHasUpdateUserId : IHasUpdateUserId<Guid>;


public interface IHasUpdateUserId<out TUser> where TUser: notnull
{
    /*
     * 在设计上，希望规避可空值类型
     */
    
    /// <summary>
    /// 更新用户Id
    /// </summary>
    TUser UpdateUserId { get; }
}