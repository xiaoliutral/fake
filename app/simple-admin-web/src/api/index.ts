/**
 * API 统一导出
 * 使用自动生成的 Service
 */

// 导出所有生成的类型和服务
export * from './generated'

// 配置 API
import { OpenAPI } from './generated/core/OpenAPI'
import axios from 'axios'
import { message } from 'ant-design-vue'
import router from '@/router'

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

// 配置全局 axios 拦截器处理错误
axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response) {
      const status = error.response.status

      if (status === 401) {
        // 动态导入避免循环依赖
        const { useUserStore } = await import('@/stores/user')
        const userStore = useUserStore()
        
        if (userStore.refreshToken) {
          // 尝试刷新 token
          const success = await userStore.refreshAccessToken()
          if (success) {
            // 重试原请求
            error.config.headers.Authorization = `Bearer ${userStore.accessToken}`
            return axios.request(error.config)
          }
        }
        
        // 刷新失败或没有 refreshToken，跳转登录
        userStore.logout()
        router.push('/login')
        message.error('登录已过期，请重新登录')
      } else if (status === 403) {
        message.error('没有权限访问')
      } else if (status === 500) {
        message.error(error.response.data?.message || '服务器错误')
      }
    }
    return Promise.reject(error)
  }
)
