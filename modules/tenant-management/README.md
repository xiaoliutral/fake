# TenantManagement

多租户管理模块。表名默认前缀 `fk_`（`Fake:DbTablePrefix` 可配置）。

## 分层

- Application → Domain（不引用 Infrastructure）
- Host 同时 DependsOn Application + Infrastructure

## 表

| 逻辑名 | 默认物理表 |
|--------|------------|
| tenant | fk_tenant |
| tenant_connection_string | fk_tenant_connection_string |
