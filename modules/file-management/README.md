# FileManagement

通用文件元数据模块。业务表只存 `FileId`；对象存储 ObjectKey = FileId。

## 分层

- Application → Domain（不引用 Infrastructure）
- Host 同时 DependsOn Application + Infrastructure

## FileService

| API | 说明 |
|-----|------|
| UploadAsync | 服务端上传 → Available |
| PresignUploadAsync / StsUploadAsync | 签发直传凭证 + Pending |
| ConfirmUploadAsync | Pending → Available |
| GetAsync / GetAccessUrlAsync / DeleteAsync | 按 FileId |

不提供按业务查询；订单/资料等自己维护 `FileId` 关联。
