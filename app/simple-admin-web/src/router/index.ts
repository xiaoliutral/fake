import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { message } from 'ant-design-vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { title: '登录', requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/layouts/BasicLayout.vue'),
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard.vue'),
        meta: { title: '仪表盘', icon: 'DashboardOutlined' }
      },
      {
        path: 'system',
        name: 'System',
        redirect: '/system/user',
        meta: { title: '系统管理', icon: 'SettingOutlined' },
        children: [
          {
            path: 'user',
            name: 'User',
            component: () => import('@/views/system/user/index.vue'),
            meta: { 
              title: '用户管理', 
              icon: 'UserOutlined',
              permission: 'Rbac.Users.Query'
            }
          },
          {
            path: 'role',
            name: 'Role',
            component: () => import('@/views/system/role/index.vue'),
            meta: { 
              title: '角色管理', 
              icon: 'TeamOutlined',
              permission: 'Rbac.Roles.Query'
            }
          },
          {
            path: 'menu',
            name: 'Menu',
            component: () => import('@/views/system/menu/index.vue'),
            meta: { 
              title: '菜单管理', 
              icon: 'MenuOutlined',
              permission: 'Rbac.Menus.Query'
            }
          }
        ]
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/NotFound.vue'),
    meta: { title: '404' }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach(async (to, from, next) => {
  const userStore = useUserStore()

  // 设置页面标题
  document.title = to.meta.title ? `${to.meta.title} - SimpleAdmin` : 'SimpleAdmin'

  // 登录页面直接放行
  if (to.path === '/login') {
    if (userStore.isLoggedIn) {
      next('/')
    } else {
      next()
    }
    return
  }

  // 检查是否需要登录
  if (to.meta.requiresAuth !== false) {
    if (!userStore.isLoggedIn) {
      message.warning('请先登录')
      next('/login')
      return
    }

    // 获取用户信息
    if (!userStore.userInfo) {
      try {
        await userStore.getCurrentUser()
      } catch (error) {
        next('/login')
        return
      }
    }

    // 检查权限
    if (to.meta.permission && !userStore.hasPermission(to.meta.permission as string)) {
      message.error('没有权限访问该页面')
      next(from.path || '/')
      return
    }
  }

  next()
})

export default router
