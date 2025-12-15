import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { AuthService, setAuthToken, clearAuthToken } from '@/api'
import type { UserInfoDto } from '@/api'
import { message } from 'ant-design-vue'
import { handleApiError } from '@/utils/error-handler'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const savedUserInfo = localStorage.getItem('userInfo')
  const userInfo = ref<UserInfoDto | null>(savedUserInfo ? JSON.parse(savedUserInfo) : null)

  const permissions = computed(() => userInfo.value?.permissions || [])
  const menus = computed(() => userInfo.value?.menus || [])
  const isLoggedIn = computed(() => !!token.value)

  // 登录
  async function login(account: string, password: string) {
    try {
      const userInfoData = await AuthService.postRbacAuthLogin(account, password)
      // 登录成功后，使用用户ID作为临时token（实际项目中应该使用JWT）
      token.value = `Bearer_${userInfoData.id}`
      userInfo.value = userInfoData
      localStorage.setItem('token', token.value)
      localStorage.setItem('userInfo', JSON.stringify(userInfoData))
      // 设置 API token
      setAuthToken(token.value)
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
      if (!userInfo.value?.id) {
        throw new Error('用户信息不存在')
      }
      const data = await AuthService.getRbacAuthGetCurrentUser(userInfo.value.id)
      userInfo.value = data
      return data
    } catch (error) {
      logout()
      throw error
    }
  }

  // 登出
  function logout() {
    token.value = ''
    userInfo.value = null
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

  return {
    token,
    userInfo,
    permissions,
    menus,
    isLoggedIn,
    login,
    getCurrentUser,
    logout,
    hasPermission,
    hasRole
  }
})
