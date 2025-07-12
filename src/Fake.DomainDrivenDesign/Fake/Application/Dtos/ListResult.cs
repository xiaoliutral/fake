namespace Fake.Application.Dtos;

/// <summary>
/// 列表结果
/// </summary>
/// <param name="items"></param>
/// <typeparam name="T"></typeparam>
public class ListResult<T>(List<T> items)
{
    public List<T> Items { get; set; } = items;
}