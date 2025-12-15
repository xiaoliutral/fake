import { message } from 'ant-design-vue'
import { ApiError } from '@/api/generated/core/ApiError'
import router from '@/router'

/**
 * 全局错误处理器
 */
export function handleApiError(error: any): void {
  if (error instanceof ApiError) {
    const { status, body } = error
    
    // 尝试从响应体中提取 message
    let errorMessage = '请求失败'
    
    if (body) {
      // 后端可能返回的格式：{ message: '错误信息' } 或 { Message: '错误信息' }
      errorMessage = body.message || body.Message || body.error || body.Error || errorMessage
    }

    switch (status) {
      case 400:
        message.error(errorMessage || '请求参数错误')
        break
      case 401:
        message.error('未授权，请重新登录')
        localStorage.removeItem('token')
        localStorage.removeItem('userInfo')
        router.push('/login')
        break
      case 403:
        message.error(errorMessage || '没有权限访问')
        break
      case 404:
        message.error(errorMessage || '请求的资源不存在')
        break
      case 500:
        message.error(errorMessage || '服务器错误')
        break
      default:
        message.error(errorMessage)
    }
  } else if (error?.message) {
    message.error(error.message)
  } else {
    message.error('未知错误')
  }
}

/**
 * 包装异步函数，自动处理错误
 */
export function withErrorHandler<T extends (...args: any[]) => Promise<any>>(
  fn: T
): (...args: Parameters<T>) => Promise<ReturnType<T> | undefined> {
  return async (...args: Parameters<T>) => {
    try {
      return await fn(...args)
    } catch (error) {
      handleApiError(error)
      return undefined
    }
  }
}
