import { useUserStore } from '@/stores/user'

/**
 * 检查是否有权限
 */
export function hasPermission(permission: string): boolean {
  const userStore = useUserStore()
  return userStore.permissions.includes(permission)
}

/**
 * 检查是否有任一权限
 */
export function hasAnyPermission(permissions: string[]): boolean {
  const userStore = useUserStore()
  return permissions.some(p => userStore.permissions.includes(p))
}

/**
 * 检查是否有所有权限
 */
export function hasAllPermissions(permissions: string[]): boolean {
  const userStore = useUserStore()
  return permissions.every(p => userStore.permissions.includes(p))
}

/**
 * 检查是否有角色
 */
export function hasRole(roleCode: string): boolean {
  const userStore = useUserStore()
  return userStore.userInfo?.roles?.some(r => r.code === roleCode) || false
}

/**
 * 检查是否有任一角色
 */
export function hasAnyRole(roleCodes: string[]): boolean {
  const userStore = useUserStore()
  return roleCodes.some(code => 
    userStore.userInfo?.roles?.some(r => r.code === code)
  ) || false
}
