using Consul;
using Fake.Consul.Configuration.Parsers;
using Fake.Helpers;
using Microsoft.Extensions.Configuration;

namespace Fake.Consul.Configuration;

public class ConsulConfigurationSource(IConsulClient consulClient, string key): IConfigurationSource
{
    /// <summary>
    /// 配置文件名称（完整的Consul_Key）
    /// </summary>
    public string Key { get; set; } = key;
    
    /// <summary>
    /// 指定阻塞请求的等待时间。最长限制为10分钟，默认是5分钟。
    /// </summary>
    public TimeSpan WaitTime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 当Consul配置数据发生变更是否重新加载配置源
    /// </summary>
    public bool ReloadOnChange { get; set; }
    
    public IConfigurationParser Parser { get; set; } = new JsonConfigurationParser();

    public Func<Exception, Task> OnLoadException { get; set; } = _ => Task.CompletedTask;

    public Func<Exception, Task> OnWatchException { get; set; } = async _ => { await Task.Delay(5); };


    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ConsulConfigurationProvider(consulClient, this);
    }
}