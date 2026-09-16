using Fake.Application;
using Fake.Domain.Exceptions;
using Fake.FileManagement.Application.Dtos;
using Fake.FileManagement.Domain.FileAggregate;
using Fake.ObjectStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.FileManagement.Application.Services;

/// <summary>
/// 文件服务：统一上传、直传确认、查询、访问地址与删除。
/// 业务表建议存 <see cref="FileDto.Id"/>（FileId），访问时再换临时 URL。
/// </summary>
[Authorize]
[ApiExplorerSettings(GroupName = "FileManagement")]
public class FileService(
    IStoredFileRepository storedFileRepository,
    IObjectStorage objectStorage,
    IOptions<FakeFileManagementOptions> options)
    : ApplicationService
{
    private readonly FakeFileManagementOptions _options = options.Value;

    /// <summary>
    /// 服务端转发上传并写入元数据（文件仍经业务服务器）。私有桶用户上传请优先用直传。
    /// </summary>
    public virtual async Task<FileDto> UploadAsync(
        IFormFile file,
        string category,
        string? bizType = null,
        string? bizId = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new DomainException("请选择要上传的文件");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        category = category.Trim().ToLowerInvariant();

        ValidateFile(file, category);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectKey = BuildObjectKey(category, extension);

        await using var stream = file.OpenReadStream();
        var saveResult = await objectStorage.SaveAsync(new ObjectStorageSaveArgs
        {
            ObjectKey = objectKey,
            Content = stream,
            ContentType = file.ContentType
        }, cancellationToken);

        var entity = new StoredFile(
            objectKey: saveResult.ObjectKey,
            fileName: file.FileName,
            category: category,
            size: saveResult.Size ?? file.Length,
            contentType: string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
            bizType: bizType,
            bizId: bizId);

        await storedFileRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);

        var dto = ObjectMapper.Map<StoredFile, FileDto>(entity);
        dto.Url = saveResult.Url;
        return dto;
    }

    /// <summary>
    /// 签发预签名 PUT：前端直传对象存储，再调用 <see cref="ConfirmUploadAsync"/> 登记元数据。
    /// </summary>
    public virtual async Task<PresignUploadDto> PresignUploadAsync(
        PresignUploadInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Category);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.FileName);

        if (!objectStorage.SupportsClientDirectUpload)
        {
            throw new DomainException($"当前存储 Provider（{objectStorage.Name}）不支持前端直传");
        }

        var category = input.Category.Trim().ToLowerInvariant();
        ValidateFileMeta(input.FileName, input.ContentType, size: null, category);

        var extension = Path.GetExtension(input.FileName).ToLowerInvariant();
        var objectKey = BuildObjectKey(category, extension);
        TimeSpan? expires = input.ExpiresSeconds is > 0
            ? TimeSpan.FromSeconds(input.ExpiresSeconds.Value)
            : null;

        var ticket = await objectStorage.CreatePresignedUploadAsync(
            new ObjectStoragePresignUploadArgs
            {
                ObjectKey = objectKey,
                ContentType = input.ContentType,
                Expires = expires
            },
            cancellationToken);

        return new PresignUploadDto
        {
            ObjectKey = ticket.ObjectKey,
            UploadUrl = ticket.UploadUrl,
            Method = ticket.Method,
            ContentType = ticket.ContentType,
            Headers = ticket.Headers,
            ExpireAt = ticket.ExpireAt
        };
    }

    /// <summary>
    /// 签发 STS 临时密钥：前端用官方 SDK 直传，再 Confirm。
    /// </summary>
    public virtual async Task<StsUploadDto> StsUploadAsync(
        StsUploadInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Category);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.FileName);

        if (!objectStorage.SupportsClientDirectUpload)
        {
            throw new DomainException($"当前存储 Provider（{objectStorage.Name}）不支持前端直传");
        }

        var category = input.Category.Trim().ToLowerInvariant();
        ValidateFileMeta(input.FileName, contentType: null, size: null, category);

        var extension = Path.GetExtension(input.FileName).ToLowerInvariant();
        var objectKey = BuildObjectKey(category, extension);
        TimeSpan? expires = input.ExpiresSeconds is > 0
            ? TimeSpan.FromSeconds(input.ExpiresSeconds.Value)
            : null;

        var ticket = await objectStorage.CreateStsUploadAsync(
            new ObjectStorageStsUploadArgs
            {
                ObjectKey = objectKey,
                Expires = expires
            },
            cancellationToken);

        return new StsUploadDto
        {
            ObjectKey = ticket.ObjectKey,
            PhysicalObjectKey = ticket.PhysicalObjectKey,
            Bucket = ticket.Bucket,
            Region = ticket.Region,
            TmpSecretId = ticket.TmpSecretId,
            TmpSecretKey = ticket.TmpSecretKey,
            SessionToken = ticket.SessionToken,
            StartTime = ticket.StartTime,
            ExpiredTime = ticket.ExpiredTime
        };
    }

    /// <summary>
    /// 直传完成后登记元数据（不接收文件流）。返回 FileId 供业务表持久化。
    /// </summary>
    public virtual async Task<FileDto> ConfirmUploadAsync(
        ConfirmUploadInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ObjectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.FileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Category);

        var category = input.Category.Trim().ToLowerInvariant();
        var objectKey = ObjectStoragePathHelper.NormalizeObjectKey(input.ObjectKey);
        ValidateFileMeta(input.FileName, input.ContentType, input.Size, category);

        if (input.VerifyExists)
        {
            var exists = await objectStorage.ExistsAsync(objectKey, cancellationToken);
            if (!exists)
            {
                throw new DomainException($"对象存储中不存在该文件，请确认直传已成功：{objectKey}");
            }
        }

        var entity = new StoredFile(
            objectKey: objectKey,
            fileName: input.FileName.Trim(),
            category: category,
            size: input.Size ?? 0,
            contentType: string.IsNullOrWhiteSpace(input.ContentType) ? null : input.ContentType.Trim(),
            bizType: input.BizType,
            bizId: input.BizId);

        await storedFileRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);

        var dto = ObjectMapper.Map<StoredFile, FileDto>(entity);
        dto.Url = await ResolveUrlAsync(entity, expires: null, cancellationToken);
        return dto;
    }

    /// <summary>
    /// 按 Id 获取文件元数据。
    /// </summary>
    public virtual async Task<FileDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        var dto = ObjectMapper.Map<StoredFile, FileDto>(entity);
        dto.Url = await ResolveUrlAsync(entity, expires: null, cancellationToken);
        return dto;
    }

    /// <summary>
    /// 按业务维度查询文件列表。
    /// </summary>
    public virtual async Task<List<FileDto>> GetListByBizAsync(
        string bizType,
        string bizId,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bizType);
        ArgumentException.ThrowIfNullOrWhiteSpace(bizId);

        var list = await storedFileRepository.GetListByBizAsync(bizType, bizId, category, cancellationToken);
        var result = new List<FileDto>(list.Count);
        foreach (var entity in list)
        {
            var dto = ObjectMapper.Map<StoredFile, FileDto>(entity);
            dto.Url = await ResolveUrlAsync(entity, expires: null, cancellationToken);
            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// 获取访问地址；传入 expires 时生成签名 URL（若存储支持）。
    /// </summary>
    public virtual async Task<string> GetAccessUrlAsync(
        Guid id,
        int? expiresSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        TimeSpan? expires = null;
        if (expiresSeconds is > 0)
        {
            expires = TimeSpan.FromSeconds(expiresSeconds.Value);
        }
        else if (ShouldUseSignedUrl(entity.Category))
        {
            expires = TimeSpan.FromSeconds(_options.DefaultSignedUrlExpiresSeconds);
        }

        return await ResolveUrlAsync(entity, expires, cancellationToken);
    }

    /// <summary>
    /// 删除文件元数据，并尝试删除对象存储中的对象。
    /// </summary>
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);

        try
        {
            await objectStorage.DeleteAsync(entity.ObjectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "删除对象存储文件失败：{ObjectKey}", entity.ObjectKey);
        }

        await storedFileRepository.DeleteAsync(entity, cancellationToken: cancellationToken);
        await UnitOfWorkManager.Current!.SaveChangesAsync(cancellationToken);
    }

    private async Task<StoredFile> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await storedFileRepository.FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            throw new DomainException($"文件不存在：{id}");
        }

        return entity;
    }

    private void ValidateFile(IFormFile file, string category)
    {
        ValidateFileMeta(file.FileName, file.ContentType, file.Length, category);
    }

    private void ValidateFileMeta(string fileName, string? contentType, long? size, string category)
    {
        var categoryOptions = _options.Categories.GetValueOrDefault(category);
        var maxSize = categoryOptions?.MaxSizeBytes ?? _options.DefaultMaxSizeBytes;
        if (maxSize > 0 && size is > 0 && size.Value > maxSize)
        {
            throw new DomainException($"文件大小不能超过 {maxSize} 字节");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowed = categoryOptions?.AllowedExtensions ?? _options.DefaultAllowedExtensions;
        if (allowed is { Length: > 0 } &&
            !allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException($"不支持的文件类型：{extension}");
        }

        _ = contentType; // 预留：可按 category 校验 MIME
    }

    private bool ShouldUseSignedUrl(string category)
    {
        return _options.Categories.TryGetValue(category, out var categoryOptions) && categoryOptions.UseSignedUrl;
    }

    private Task<string> ResolveUrlAsync(StoredFile entity, TimeSpan? expires, CancellationToken cancellationToken)
    {
        if (expires == null && ShouldUseSignedUrl(entity.Category))
        {
            expires = TimeSpan.FromSeconds(_options.DefaultSignedUrlExpiresSeconds);
        }

        return objectStorage.GetUrlAsync(entity.ObjectKey, expires, cancellationToken);
    }

    private static string BuildObjectKey(string category, string extension)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        return $"{category}/{date}/{Guid.NewGuid():N}{extension}";
    }
}
