namespace Fake.Application.Dtos;

/// <summary>
/// 列表响应结果模版
/// </summary>
/// <param name="items"></param>
/// <typeparam name="T"></typeparam>
public class ListResponse<T>(List<T> items)
{
    public List<T> Items { get; set; } = items;
}