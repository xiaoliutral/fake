## OpenFGA .NET 8 权限系统 Demo

参考 OpenFGA 官方文档: [OpenFGA 简介](https://openfga.dev/docs/fga)

### 目录结构

```
OpenFgaDemo.sln
OpenFgaDemo.Api/
  Program.cs
  appsettings.json
docker-compose.yml
```

### 启动 OpenFGA 服务（本地）

```bash
docker compose up -d
```

访问 Playground: http://localhost:3000

### 运行 API（需要本机安装 .NET 8 SDK）

```bash
cd OpenFgaDemo.Api
dotnet run
```

Swagger: http://localhost:5187/swagger

### 快速体验

1) 创建 Store（获得 `store_id`）

POST http://localhost:5187/stores

响应中保存 `id`，然后在 `OpenFgaDemo.Api/appsettings.json` 中设置 `OpenFGA:StoreId`。

2) 配置授权模型（示例：简单的文档编辑权限）

POST http://localhost:5187/model/configure

Body 示例：

```json
{
  "schemaVersion": "1.1",
  "typeDefinitions": [
    {
      "type": "user"
    },
    {
      "type": "document",
      "relations": {
        "viewer": { "this": {} },
        "editor": { "this": {} }
      },
      "permissions": {
        "read": { "anyOf": [ { "relation": "viewer" }, { "relation": "editor" } ] },
        "write": { "relation": "editor" }
      }
    }
  ]
}
```

3) 写入关系（赋权）

POST http://localhost:5187/tuples/write

```json
{
  "writes": [
    { "user": "user:alice", "relation": "editor", "object": "document:doc1" },
    { "user": "user:bob",   "relation": "viewer", "object": "document:doc1" }
  ]
}
```

4) 检查权限（Check）

POST http://localhost:5187/check

```json
{ "user": "user:alice", "relation": "write", "object": "document:doc1" }
```

期望返回 `allowed: true`。

5) 列出对象（List Objects）

POST http://localhost:5187/list-objects

```json
{ "user": "user:bob", "type": "document", "relation": "read" }
``;

### 说明

- 该 Demo 使用 OpenFGA HTTP API 直接调用，便于理解协议与数据结构。
- 可根据需要替换为官方 .NET SDK；本文实现对齐官方 API 概念：Store、Authorization Model、Tuples、Check、List Objects、List Users。


