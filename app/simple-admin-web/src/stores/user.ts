import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { AuthService, setAuthToken, clearAuthToken } from '@/api'
import type { UserInfoDto, LoginResultDto } from '@/api'
import { message } from 'ant-design-vue'
import { handleApiError } from '@/utils/error-handler'

// Token 存储 key
const TOKEN_KEY = 'access_token'
const REFRESH_TOKEN_KEY = 'refresh_token'
const TOKEN_EXPIRES_KEY = 'token_expires_at'
const USER_INFO_KEY = 'user_info'

export const useUserStore = defineStore('user', () => {
  const accessToken = ref<string>(localStorage.getItem(TOKEN_KEY) || '')
  const refreshToken = ref<string>(localStorage.getItem(REFRESH_TOKEN_KEY) || '')
  const tokenExpiresAt = ref<number>(Number(localStorage.getItem(TOKEN_EXPIRES_KEY)) || 0)
  const savedUserInfo = localStorage.getItem(USER_INFO_KEY)
  const userInfo = ref<UserInfoDto | null>(savedUserInfo ? JSON.parse(savedUserInfo) : null)

  const permissions = computed(() => userInfo.value?.permissions || [])
  const menus = computed(() => userInfo.value?.menus || [])
  const isLoggedIn = computed(() => !!accessToken.value)
  // 兼容旧代码
  const token = computed(() => accessToken.value)

  // 保存 Token 信息
  function saveTokens(result: LoginResultDto) {
    accessToken.value = result.accessToken || ''
    refreshToken.value = result.refreshToken || ''
    // 计算过期时间（提前5分钟刷新）
    tokenExpiresAt.value = Date.now() + ((result.expiresIn || 3600) - 300) * 1000
    
    localStorage.setItem(TOKEN_KEY, accessToken.value)
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken.value)
    localStorage.setItem(TOKEN_EXPIRES_KEY, String(tokenExpiresAt.value))
    
    // 设置 API token
    if (accessToken.value) {
      setAuthToken(accessToken.value)
    }
  }

  // 检查 Token 是否需要刷新
  function isTokenExpiringSoon(): boolean {
    return tokenExpiresAt.value > 0 && Date.now() >= tokenExpiresAt.value
  }

  // 刷新 Token
  async function refreshAccessToken(): Promise<boolean> {
    if (!refreshToken.value) {
      return false
    }
    
    try {
      const result = await AuthService.postRbacAuthRefreshToken({ refreshToken: refreshToken.value })
      saveTokens(result)
      return true
    } catch {
      // 刷新失败，清除登录状态
      logout()
      return false
    }
  }

  // 登录
  async function login(account: string, password: string) {
    try {
      const result = await AuthService.postRbacAuthLogin({ account, password })
      saveTokens(result)
      
      // 获取用户完整信息
      await getCurrentUser()
      
      message.success('登录成功')
      return true
    } catch (error) {
      handleApiError(error)
      return false
    }
  }

  // 获取当前用户信息
  async function getCurrentUser() {
    try {
      // 检查是否需要刷新 Token
      if (isTokenExpiringSoon()) {
        await refreshAccessToken()
      }
      
      const data = await AuthService.getRbacAuthGetCurrentUser()
      userInfo.value = data
      localStorage.setItem(USER_INFO_KEY, JSON.stringify(data))
      return data
    } catch (error) {
      logout()
      throw error
    }
  }

  // 刷新用户信息（强制从服务器获取最新数据）
  async function refreshUserInfo() {
    try {
      const data = await AuthService.getRbacAuthGetCurrentUser()
      userInfo.value = data
      localStorage.setItem(USER_INFO_KEY, JSON.stringify(data))
      return data
    } catch (error) {
      handleApiError(error)
      throw error
    }
  }

  // 登出
  function logout() {
    accessToken.value = ''
    refreshToken.value = ''
    tokenExpiresAt.value = 0
    userInfo.value = null
    
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    localStorage.removeItem(TOKEN_EXPIRES_KEY)
    localStorage.removeItem(USER_INFO_KEY)
    // 兼容旧代码
    localStorage.removeItem('token')
    localStorage.removeItem('userInfo')
    
    // 清除 API token
    clearAuthToken()
  }

  // 检查权限
  function hasPermission(permission: string): boolean {
    return permissions.value.includes(permission)
  }

  // 检查角色
  function hasRole(roleCode: string): boolean {
    return userInfo.value?.roles?.some(r => r.code === roleCode) || false
  }

  // 初始化：恢复登录状态
  function initAuth() {
    if (accessToken.value) {
      setAuthToken(accessToken.value)
    }
  }

  // 自动初始化
  initAuth()

  return {
    token,
    accessToken,
    refreshToken,
    userInfo,
    permissions,
    menus,
    isLoggedIn,
    login,
    getCurrentUser,
    refreshUserInfo,
    logout,
    hasPermission,
    hasRole,
    refreshAccessToken,
    isTokenExpiringSoon
  }
})
