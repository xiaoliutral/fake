using System.ComponentModel.DataAnnotations;

namespace Fake.Validation;

public interface IObjectValidator
{
    /// <summary>
    /// 校验对象的入参列表
    /// </summary>
    /// <param name="validatingObject"></param>
    /// <param name="name"></param>
    /// <param name="allowNull"></param>
    /// <returns></returns>
    Task ValidateAsync(object validatingObject, string? name = null, bool allowNull = false);

    /// <summary>
    /// 获取从对象校验出的异常
    /// </summary>
    /// <param name="validatingObject"></param>
    /// <param name="name"></param>
    /// <param name="allowNull"></param>
    /// <returns></returns>
    Task<List<ValidationResult>> GetErrorsAsync(object validatingObject, string? name = null, bool allowNull = false);
}