namespace Fake.ObjectStorage;

public class LocalObjectStorageOptions
{
    /// <summary>
    /// 本地根目录。相对路径相对 ContentRoot。
    /// </summary>
    public string BasePath { get; set; } = "wwwroot/uploads";

    /// <summary>
    /// 对外访问前缀，例如 /uploads。
    /// </summary>
    public string BaseUrl { get; set; } = "/uploads";
}
