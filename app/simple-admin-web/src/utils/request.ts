import axios, { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import { message } from 'ant-design-vue'
import { useUserStore } from '@/stores/user'
import router from '@/router'

const request = axios.create({
  baseURL: '/rbac',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 是否正在刷新 Token
let isRefreshing = false
// 等待刷新的请求队列
let refreshSubscribers: ((token: string) => void)[] = []

// 添加请求到等待队列
function subscribeTokenRefresh(callback: (token: string) => void) {
  refreshSubscribers.push(callback)
}

// 通知所有等待的请求
function onTokenRefreshed(token: string) {
  refreshSubscribers.forEach(callback => callback(token))
  refreshSubscribers = []
}

// 请求拦截器
request.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    const userStore = useUserStore()
    
    // 如果有 Token，添加到请求头
    if (userStore.accessToken) {
      config.headers.Authorization = `Bearer ${userStore.accessToken}`
    }
    
    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  }
)

// 响应拦截器
request.interceptors.response.use(
  (response: AxiosResponse) => {
    return response.data
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }
    
    if (error.response) {
      const status = error.response.status
      const data: any = error.response.data

      // 401 错误处理：尝试刷新 Token
      if (status === 401 && !originalRequest._retry) {
        const userStore = useUserStore()
        
        // 如果没有 refreshToken，直接登出
        if (!userStore.refreshToken) {
          userStore.logout()
          router.push('/login')
          return Promise.reject(error)
        }

        // 如果正在刷新，将请求加入队列
        if (isRefreshing) {
          return new Promise(resolve => {
            subscribeTokenRefresh((token: string) => {
              originalRequest.headers.Authorization = `Bearer ${token}`
              resolve(request(originalRequest))
            })
          })
        }

        originalRequest._retry = true
        isRefreshing = true

        try {
          // 尝试刷新 Token
          const success = await userStore.refreshAccessToken()
          
          if (success) {
            // 刷新成功，重试原请求
            onTokenRefreshed(userStore.accessToken)
            originalRequest.headers.Authorization = `Bearer ${userStore.accessToken}`
            return request(originalRequest)
          } else {
            // 刷新失败，登出
            userStore.logout()
            router.push('/login')
            return Promise.reject(error)
          }
        } catch {
          userStore.logout()
          router.push('/login')
          return Promise.reject(error)
        } finally {
          isRefreshing = false
        }
      }

      switch (status) {
        case 401:
          message.error('未授权，请重新登录')
          break
        case 403:
          message.error('没有权限访问')
          break
        case 404:
          message.error('请求的资源不存在')
          break
        case 500:
          message.error(data?.message || '服务器错误')
          break
        default:
          message.error(data?.message || '请求失败')
      }
    } else if (error.request) {
      message.error('网络错误，请检查网络连接')
    } else {
      message.error('请求配置错误')
    }
    return Promise.reject(error)
  }
)

export default request
