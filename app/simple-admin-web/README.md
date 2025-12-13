# SimpleAdmin Web

基于 Vue 3 + TypeScript + Ant Design Vue 的简单管理系统前端。

## 技术栈

- **Vue 3** - 渐进式 JavaScript 框架
- **TypeScript** - JavaScript 的超集
- **Vite** - 下一代前端构建工具
- **Ant Design Vue** - 企业级 UI 组件库
- **Vue Router** - 官方路由管理器
- **Pinia** - Vue 状态管理库
- **Axios** - HTTP 客户端
- **Day.js** - 轻量级日期处理库

## 功能特性

- ✅ 用户登录/登出
- ✅ 用户管理（CRUD、分配角色）
- ✅ 角色管理（CRUD、分配权限）
- ✅ 菜单管理（树形结构、CRUD）
- ✅ 权限控制（路由权限、按钮权限）
- ✅ 响应式布局
- ✅ 优雅的 UI 设计

## 开始使用

### 安装依赖

```bash
npm install
```

### 开发模式

```bash
npm run dev
```

访问 http://localhost:5173

### 构建生产版本

```bash
npm run build
```

### 预览生产版本

```bash
npm run preview
```

## 默认账号

- 账号：admin
- 密码：123456

## 项目结构

```
src/
├── api/              # API 接口
├── assets/           # 静态资源
├── components/       # 公共组件
├── layouts/          # 布局组件
├── router/           # 路由配置
├── stores/           # 状态管理
├── styles/           # 全局样式
├── types/            # TypeScript 类型定义
├── utils/            # 工具函数
├── views/            # 页面组件
├── App.vue           # 根组件
└── main.ts           # 入口文件
```

## 权限控制

### 路由权限

在路由配置中添加 `meta.permission` 字段：

```typescript
{
  path: '/system/user',
  meta: { permission: 'Rbac.Users.Query' }
}
```

### 按钮权限

使用 `hasPermission` 函数：

```vue
<a-button v-if="hasPermission('Rbac.Users.Create')">
  新增用户
</a-button>
```

## API 配置

API 基础地址配置在 `vite.config.ts` 中：

```typescript
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      changeOrigin: true
    }
  }
}
```

## 许可证

MIT
