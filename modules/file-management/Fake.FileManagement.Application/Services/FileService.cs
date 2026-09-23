using Fake.Application;
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
/// 通用文件服务：元数据 + 对象存储。业务表只存 FileId，不在本模块挂业务外键。
/// ObjectKey = FileId。
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
    /// 服务端转发上传并写入 Available 元数据。
    /// </summary>
    public virtual async Task<FileDto> UploadAsync(
        IFormFile file,
        string? policy = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new BusinessException("请选择要上传的文件");
        }

        ValidateFileMeta(file.FileName, file.ContentType, file.Length, policy);

        await using var stream = file.OpenReadStream();
        var saveResult = await objectStorage.SaveAsync(new ObjectStorageSaveArgs
        {
            ObjectKey = StoredFile.GenerateObjectKey(),
            Content = stream,
            ContentType = file.ContentType
        }, cancellationToken);

        var entity = new StoredFile(
            fileName: file.FileName,
            status: StoredFileStatus.Available,
            size: saveResult.Size ?? file.Length,
            contentType: string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
            storageSource: _options.DefaultStorageSource);

        await storedFileRepository.InsertAsync(entity, cancellationToken: cancellationToken);

        return ToDto(entity, saveResult.Url);
    }

    /// <summary>
    /// 签发预签名 PUT，并创建 Pending 文件记录。直传完成后调用 Confirm。
    /// </summary>
    public virtual async Task<PresignUploadDto> PresignUploadAsync(
        PresignUploadInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.FileName);

        if (!objectStorage.SupportsClientDirectUpload)
        {
            throw new BusinessException($"当前存储 Provider（{objectStorage.Name}）不支持前端直传");
        }

        ValidateFileMeta(input.FileName, input.ContentType, size: null, input.Policy);

        var entity = new StoredFile(
            fileName: input.FileName,
            status: StoredFileStatus.Pending,
            size: 0,
            contentType: input.ContentType,
            storageSource: _options.DefaultStorageSource);

        await storedFileRepository.InsertAsync(entity, cancellationToken: cancellationToken);

        TimeSpan? expires = input.ExpiresSeconds is > 0
            ? TimeSpan.FromSeconds(input.ExpiresSeconds.Value)
            : null;

        var ticket = await objectStorage.CreatePresignedUploadAsync(
            new ObjectStoragePresignUploadArgs
            {
                ObjectKey = entity.ObjectKey,
                ContentType = input.ContentType,
                Expires = expires
            },
            cancellationToken);

        return new PresignUploadDto
        {
            FileId = entity.Id,
            UploadUrl = ticket.UploadUrl,
            Method = ticket.Method,
            ContentType = ticket.ContentType,
            Headers = ticket.Headers is { Count: > 0 } ? ticket.Headers : null,
            ExpireAt = ticket.ExpireAt
        };
    }

    /// <summary>
    /// 签发 STS，并创建 Pending 文件记录。直传完成后调用 Confirm。
    /// </summary>
    public virtual async Task<StsUploadDto> StsUploadAsync(
        StsUploadInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.FileName);

        if (!objectStorage.SupportsClientDirectUpload)
        {
            throw new BusinessException($"当前存储 Provider（{objectStorage.Name}）不支持前端直传");
        }

        ValidateFileMeta(input.FileName, contentType: null, size: null, input.Policy);


        var entity = new StoredFile(
            fileName: input.FileName,
            status: StoredFileStatus.Pending,
            size: 0,
            contentType: null,
            storageSource: _options.DefaultStorageSource);

        await storedFileRepository.InsertAsync(entity, cancellationToken: cancellationToken);

        TimeSpan? expires = input.ExpiresSeconds is > 0
            ? TimeSpan.FromSeconds(input.ExpiresSeconds.Value)
            : null;

        var ticket = await objectStorage.CreateStsUploadAsync(
            new ObjectStorageStsUploadArgs
            {
                ObjectKey = entity.ObjectKey,
                Expires = expires
            },
            cancellationToken);

        return new StsUploadDto
        {
            FileId = entity.Id,
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
    /// 直传完成后将 Pending 转为 Available。
    /// </summary>
    public virtual async Task<FileDto> ConfirmUploadAsync(
        ConfirmUploadInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.FileId == Guid.Empty)
        {
            throw new BusinessException("FileId 无效");
        }

        var entity = await GetRequiredAsync(input.FileId, cancellationToken);
        EnsureOwner(entity);

        if (entity.Status != StoredFileStatus.Pending)
        {
            throw new BusinessException($"文件状态不是 Pending，无法确认：{entity.Status}");
        }

        var fileName = string.IsNullOrWhiteSpace(input.FileName) ? entity.FileName : input.FileName.Trim();
        ValidateFileMeta(fileName, input.ContentType, input.Size, policy: null);

        if (input.VerifyExists)
        {
            var exists = await objectStorage.ExistsAsync(entity.ObjectKey, cancellationToken);
            if (!exists)
            {
                throw new BusinessException("对象存储中不存在该文件，请确认直传已成功");
            }
        }

        entity.MarkAvailable(
            fileName,
            input.Size ?? 0,
            string.IsNullOrWhiteSpace(input.ContentType) ? null : input.ContentType.Trim());

        await storedFileRepository.UpdateAsync(entity, cancellationToken: cancellationToken);

        var url = await ResolveUrlAsync(entity, expires: null, cancellationToken);
        return ToDto(entity, url);
    }

    public virtual async Task<FileDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);

        if (entity.Status == StoredFileStatus.Pending)
        {
            EnsureOwner(entity);
            return ToDto(entity, url: null);
        }

        var url = await ResolveUrlAsync(entity, expires: null, cancellationToken);
        return ToDto(entity, url);
    }

    public virtual async Task<string> GetAccessUrlAsync(
        Guid id,
        int? expiresSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        if (entity.Status != StoredFileStatus.Available)
        {
            throw new BusinessException("文件尚未确认，无法获取访问地址");
        }

        TimeSpan? expires = null;
        if (expiresSeconds is > 0)
        {
            expires = TimeSpan.FromSeconds(expiresSeconds.Value);
        }
        else if (_options.SignUrlsByDefault)
        {
            expires = TimeSpan.FromSeconds(_options.DefaultSignedUrlExpiresSeconds);
        }

        return await ResolveUrlAsync(entity, expires, cancellationToken);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        EnsureOwner(entity);

        try
        {
            await objectStorage.DeleteAsync(entity.ObjectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "删除对象存储文件失败：{ObjectKey}", entity.ObjectKey);
        }

        await storedFileRepository.DeleteAsync(entity, cancellationToken: cancellationToken);
    }

    private void EnsureOwner(StoredFile entity)
    {
        var userId = CurrentUser.Id;
        if (userId == null || entity.CreateUserId != userId.Value)
        {
            throw new BusinessException("无权操作该文件");
        }
    }

    private async Task<StoredFile> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await storedFileRepository.FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            throw new BusinessException($"文件不存在：{id}");
        }

        return entity;
    }

    private void ValidateFileMeta(string fileName, string? contentType, long? size, string? policy)
    {
        FileUploadPolicyOptions? policyOptions = null;
        if (!string.IsNullOrWhiteSpace(policy))
        {
            _options.Policies.TryGetValue(policy.Trim(), out policyOptions);
        }

        var maxSize = policyOptions?.MaxSizeBytes ?? _options.DefaultMaxSizeBytes;
        if (maxSize > 0 && size is > 0 && size.Value > maxSize)
        {
            throw new BusinessException($"文件大小不能超过 {maxSize} 字节");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowed = policyOptions?.AllowedExtensions ?? _options.DefaultAllowedExtensions;
        if (allowed is { Length: > 0 } &&
            !allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException($"不支持的文件类型：{extension}");
        }

        _ = contentType;
    }

    private Task<string> ResolveUrlAsync(StoredFile entity, TimeSpan? expires, CancellationToken cancellationToken)
    {
        if (expires == null && _options.SignUrlsByDefault)
        {
            expires = TimeSpan.FromSeconds(_options.DefaultSignedUrlExpiresSeconds);
        }

        return objectStorage.GetUrlAsync(entity.ObjectKey, expires, cancellationToken);
    }

    private static FileDto ToDto(StoredFile entity, string? url) => new()
    {
        Id = entity.Id,
        FileName = entity.FileName,
        ContentType = entity.ContentType,
        Size = entity.Size,
        Url = url
    };
}
