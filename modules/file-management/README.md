# FileManagement

统一文件上传与元数据管理模块，基于 `IObjectStorage`。

业务表建议持久化 **FileId**（`StoredFile.Id`），展示时再换临时访问地址。

## Services

`FileService`：

| 方法 | 说明 |
|---|---|
| `UploadAsync` | 服务端转发上传（文件经业务服务器，兼容用） |
| `PresignUploadAsync` | 预签名 PUT：前端直传对象存储 |
| `StsUploadAsync` | STS 临时密钥：前端 SDK 直传 |
| `ConfirmUploadAsync` | **直传完成后只登记元数据**（不收文件流），返回 FileId |
| `GetAsync` / `GetListByBizAsync` | 查元数据（含临时 Url） |
| `GetAccessUrlAsync` | 按 FileId 换临时访问地址 |
| `DeleteAsync` | 删元数据 + 尝试删对象 |

## 私有桶直传流程

1. `PresignUploadAsync` / `StsUploadAsync` → 拿到 `objectKey` + 上传凭证  
2. 前端 PUT / SDK 直传 COS（不经 K3s）  
3. `ConfirmUploadAsync({ objectKey, fileName, category, ... })` → 得到 `id`（FileId）  
4. 业务表只存 `FileId`；需要展示时 `GetAccessUrlAsync(fileId)` 或 `GetAsync`

## 配置要点

```json
{
  "ObjectStorage": {
    "Provider": "TencentCos",
    "KeyPrefix": "prod",
    "SignUrlsByDefault": true,
    "DefaultSignedUrlExpiresSeconds": 3600,
    "DefaultUploadExpiresSeconds": 600,
    "TencentCos": {
      "Region": "ap-guangzhou",
      "SecretId": "...",
      "SecretKey": "...",
      "Bucket": "example-1250000000",
      "BaseUrl": "https://cdn.example.com",
      "UseHttps": true
    }
  },
  "FileManagement": {
    "DefaultSignedUrlExpiresSeconds": 3600,
    "Categories": {
      "avatar": { "MaxSizeBytes": 5242880, "UseSignedUrl": true },
      "waybill": { "AllowedExtensions": [".pdf", ".png"], "UseSignedUrl": true }
    }
  }
}
```

- `SignUrlsByDefault`：私有桶建议 `true`，`GetUrlAsync` 默认返回临时读签名。  
- Cos 自定义域名会自动 `signHost`，避免签名与访问域名不一致。
