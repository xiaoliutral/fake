/**
 * API 统一导出
 * 使用自动生成的 Service
 */

// 导出所有生成的类型和服务
export * from './generated'

// 配置 API
import { OpenAPI } from './generated/core/OpenAPI'
import { axiosInstance } from './axios-config'

// 设置基础 URL 为空，使用 Vite proxy
OpenAPI.BASE = ''

// 认证 token 管理
export function setAuthToken(token: string) {
  OpenAPI.TOKEN = token
}

export function clearAuthToken() {
  OpenAPI.TOKEN = undefined
}

// 获取当前 token
export function getAuthToken() {
  return OpenAPI.TOKEN
}

// 导出 axios 实例供其他地方使用
export { axiosInstance }
