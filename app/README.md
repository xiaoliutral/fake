# SimpleAdmin - 简单管理系统

基于 Fake 框架和 Vue 3 的现代化管理系统，提供完整的 RBAC 权限管理功能。

## 项目结构

```
app/
├── SimpleAdmin.Api/          # 后端 API（.NET 8.0 + Fake Framework）
└── simple-admin-web/         # 前端 Web（Vue 3 + TypeScript + Ant Design Vue）
```

## 技术栈

### 后端

- **.NET 8.0** - 最新的 .NET 平台
- **Fake Framework** - 模块化应用框架
- **Entity Framework Core** - ORM 框架
- **SQL Server** - 数据库
- **JWT** - 身份认证
- **Swagger** - API 文档

### 前端

- **Vue 3** - 渐进式 JavaScript 框架
- **TypeScript** - JavaScript 的超集
- **Vite** - 下一代前端构建工具
- **Ant Design Vue** - 企业级 UI 组件库
- **Vue Router** - 路由管理
- **Pinia** - 状态管理
- **Axios** - HTTP 客户端

## 功能特性

### 权限管理

- ✅ 用户管理（CRUD、分配角色、批量操作）
- ✅ 角色管理（CRUD、分配权限）
- ✅ 菜单管理（树形结构、动态菜单）
- ✅ 权限控制（路由权限、按钮权限）

### 系统特性

- ✅ JWT 身份认证
- ✅ 自动 API 生成
- ✅ 响应式布局
- ✅ 优雅的 UI 设计
- ✅ 完整的 TypeScript 类型支持

## 快速开始

### 前置要求

- .NET 8.0 SDK
- Node.js 18+
- SQL Server (LocalDB 或完整版)

### 1. 启动后端

```bash
cd SimpleAdmin.Api

# 配置数据库连接字符串（appsettings.json）

# 运行数据库迁移
dotnet ef migrations add InitialCreate --project ../../modules/rbac/Fake.Rbac.Infrastructure
dotnet ef database update --project ../../modules/rbac/Fake.Rbac.Infrastructure

# 运行项目
dotnet run
```

后端将在 http://localhost:5000 启动，Swagger 文档在根路径。

### 2. 启动前端

```bash
cd simple-admin-web

# 安装依赖
npm install

# 运行开发服务器
npm run dev
```

前端将在 http://localhost:5173 启动。

### 3. 登录系统

默认管理员账号：
- 账号：admin
- 密码：123456

**⚠️ 重要：请在生产环境中立即修改默认密码！**

## 项目截图

### 登录页面
- 优雅的渐变背景
- 简洁的登录表单

### 仪表盘
- 统计卡片展示
- 用户信息展示
- 快速操作入口

### 用户管理
- 用户列表（分页、搜索）
- 新增/编辑用户
- 分配角色
- 批量删除

### 角色管理
- 角色列表
- 新增/编辑角色
- 权限树分配

### 菜单管理
- 树形菜单展示
- 新增/编辑菜单
- 支持菜单和按钮两种类型

## 开发指南

### 后端开发

1. **添加新的应用服务**

继承 `ApplicationService` 即可自动生成 API：

```csharp
public class ProductService : ApplicationService, IProductService
{
    public async Task<ProductDto> GetAsync(Guid id)
    {
        // 自动生成 GET /api/product/{id}
    }
}
```

2. **权限定义**

实现 `IPermissionDefinitionProvider`：

```csharp
public class MyPermissionProvider : IPermissionDefinitionProvider
{
    public List<PermissionDefinition> GetPermissions()
    {
        return new List<PermissionDefinition>
        {
            new("MyModule.Products", "产品管理"),
            new("MyModule.Products.Create", "创建产品", "MyModule.Products")
        };
    }
}
```

### 前端开发

1. **添加新页面**

在 `src/views` 下创建页面组件，在 `router/index.ts` 中添加路由。

2. **权限控制**

```vue
<a-button v-if="hasPermission('Rbac.Users.Create')">
  新增用户
</a-button>
```

3. **API 调用**

在 `src/api` 下创建 API 服务：

```typescript
export const productApi = {
  getList: (params) => request.get('/api/product/list', { params }),
  create: (data) => request.post('/api/product', data)
}
```

## 部署

### 后端部署

```bash
cd SimpleAdmin.Api
dotnet publish -c Release -o ./publish
```

### 前端部署

```bash
cd simple-admin-web
npm run build
```

构建产物在 `dist` 目录。

## 许可证

MIT

## 致谢

- [Fake Framework](../../README.md) - 模块化应用框架
- [Ant Design Vue](https://antdv.com/) - UI 组件库
- [Vue.js](https://vuejs.org/) - 前端框架
