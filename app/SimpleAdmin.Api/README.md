# SimpleAdmin API

基于 Fake 框架的简单管理系统后端 API。

## 技术栈

- **.NET 8.0** - 最新的 .NET 平台
- **Fake Framework** - 模块化应用框架
- **Entity Framework Core** - ORM 框架
- **SQL Server** - 数据库
- **JWT** - 身份认证
- **Swagger** - API 文档

## 功能特性

- ✅ 模块化架构
- ✅ 自动 API 生成（ApplicationService 自动转换为 Controller）
- ✅ RBAC 权限管理
- ✅ JWT 认证
- ✅ Swagger 文档
- ✅ 依赖注入
- ✅ 工作单元
- ✅ 审计日志

## 开始使用

### 前置要求

- .NET 8.0 SDK
- SQL Server (LocalDB 或完整版)

### 配置数据库

修改 `appsettings.json` 中的连接字符串：

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\mssqllocaldb;Database=SimpleAdminDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add InitialCreate --project ../modules/rbac/Fake.Rbac.Infrastructure

# 更新数据库
dotnet ef database update --project ../modules/rbac/Fake.Rbac.Infrastructure
```

### 运行项目

```bash
dotnet run
```

访问 http://localhost:5000 查看 Swagger 文档。

## 默认账号

系统会自动创建默认管理员账号：

- 账号：admin
- 密码：123456
- 角色：超级管理员

**⚠️ 重要：请在生产环境中立即修改默认密码！**

## API 端点

### 认证 API

- `POST /api/auth/login` - 用户登录
- `GET /api/auth/current` - 获取当前用户信息
- `PUT /api/auth/password` - 修改密码

### 用户管理 API

- `GET /api/rbac/user/list` - 获取用户列表
- `GET /api/rbac/user/{id}` - 获取用户详情
- `POST /api/rbac/user` - 创建用户
- `PUT /api/rbac/user/{id}` - 更新用户
- `DELETE /api/rbac/user/{id}` - 删除用户
- `PUT /api/rbac/user/{id}/roles` - 分配角色
- `GET /api/rbac/user/{id}/permissions` - 获取用户权限

### 角色管理 API

- `GET /api/rbac/role/list` - 获取角色列表
- `GET /api/rbac/role/all` - 获取所有角色
- `GET /api/rbac/role/{id}` - 获取角色详情
- `POST /api/rbac/role` - 创建角色
- `PUT /api/rbac/role/{id}` - 更新角色
- `DELETE /api/rbac/role/{id}` - 删除角色
- `PUT /api/rbac/role/{id}/permissions` - 分配权限

### 菜单管理 API

- `GET /api/rbac/menu/tree` - 获取菜单树
- `GET /api/rbac/menu/user/{userId}` - 获取用户菜单
- `GET /api/rbac/menu/{id}` - 获取菜单详情
- `POST /api/rbac/menu` - 创建菜单
- `PUT /api/rbac/menu/{id}` - 更新菜单
- `DELETE /api/rbac/menu/{id}` - 删除菜单

### 权限管理 API

- `GET /api/rbac/permission/all` - 获取所有权限
- `GET /api/rbac/permission/tree` - 获取权限树

## 项目结构

```
SimpleAdmin.Api/
├── Controllers/          # 控制器（手动创建的）
├── Program.cs           # 程序入口
├── SimpleAdminApiModule.cs  # 主模块配置
├── appsettings.json     # 配置文件
└── README.md
```

## Fake 框架特性

### 自动 API 生成

所有继承自 `ApplicationService` 的服务会自动生成对应的 API 端点，无需手动创建 Controller。

### 模块化

通过 `[DependsOn]` 特性声明模块依赖：

```csharp
[DependsOn(
    typeof(FakeAspNetCoreModule),
    typeof(FakeRbacApplicationModule)
)]
public class SimpleAdminApiModule : FakeModule
{
    // ...
}
```

### 依赖注入

框架自动注册服务，支持：
- `ITransientDependency` - 瞬时
- `IScopedDependency` - 作用域
- `ISingletonDependency` - 单例

### 工作单元

应用服务方法自动包装在工作单元中，支持事务管理。

## JWT 配置

JWT 配置在 `appsettings.json` 中：

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "ExpiryInMinutes": 60
  }
}
```

## CORS 配置

默认允许的前端地址：
- http://localhost:3000
- http://localhost:5173

可在 `SimpleAdminApiModule.cs` 中修改。

## 许可证

MIT
