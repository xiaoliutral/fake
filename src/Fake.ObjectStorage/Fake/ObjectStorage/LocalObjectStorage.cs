using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fake.ObjectStorage;

public class LocalObjectStorage(
    IOptions<FakeObjectStorageOptions> options,
    IHostEnvironment hostEnvironment)
    : ObjectStorageBase(options)
{
    public override string Name => "Local";

    protected override async Task SaveCoreAsync(ObjectStorageSaveArgs args, CancellationToken cancellationToken)
    {
        var filePath = GetPhysicalPath(args.ObjectKey);
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);

        if (!args.Overwrite && File.Exists(filePath))
        {
            throw new FakeException($"对象已存在：{args.ObjectKey}");
        }

        await using var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        if (args.Content.CanSeek)
        {
            args.Content.Position = 0;
        }

        await args.Content.CopyToAsync(fileStream, cancellationToken);
    }

    protected override Task<Stream> GetCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = GetPhysicalPath(objectKey);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"对象不存在：{objectKey}", filePath);
        }

        Stream stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult(stream);
    }

    protected override Task DeleteCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = GetPhysicalPath(objectKey);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    protected override Task<bool> ExistsCoreAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(GetPhysicalPath(objectKey)));
    }

    protected override Task<string> GetUrlCoreAsync(
        string objectKey,
        TimeSpan expires,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Local 不签名；expires 忽略
        var url = ObjectStoragePathHelper.CombineUrl(Options.Local.BaseUrl, objectKey);
        return Task.FromResult(url);
    }

    private string GetPhysicalPath(string objectKey)
    {
        var basePath = Options.Local.BasePath;
        if (!Path.IsPathRooted(basePath))
        {
            basePath = Path.Combine(hostEnvironment.ContentRootPath, basePath);
        }

        var fullPath = Path.GetFullPath(Path.Combine(basePath, objectKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootFullPath = Path.GetFullPath(basePath);
        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new FakeException("非法对象路径");
        }

        return fullPath;
    }
}
