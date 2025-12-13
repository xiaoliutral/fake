<template>
  <a-layout class="basic-layout">
    <a-layout-sider
      v-model:collapsed="appStore.collapsed"
      :trigger="null"
      collapsible
      :width="220"
      class="layout-sider"
    >
      <div class="logo">
        <span v-if="!appStore.collapsed" class="logo-text">SimpleAdmin</span>
        <span v-else class="logo-text-mini">SA</span>
      </div>
      <a-menu
        v-model:selectedKeys="selectedKeys"
        v-model:openKeys="openKeys"
        mode="inline"
        theme="dark"
        :items="menuItems"
        @click="handleMenuClick"
      />
    </a-layout-sider>

    <a-layout>
      <a-layout-header class="layout-header">
        <div class="header-left">
          <menu-unfold-outlined
            v-if="appStore.collapsed"
            class="trigger"
            @click="appStore.toggleCollapsed"
          />
          <menu-fold-outlined
            v-else
            class="trigger"
            @click="appStore.toggleCollapsed"
          />
        </div>

        <div class="header-right">
          <a-dropdown>
            <div class="user-info">
              <a-avatar :size="32" :src="userStore.userInfo?.avatar">
                <template #icon><user-outlined /></template>
              </a-avatar>
              <span class="user-name">{{ userStore.userInfo?.name }}</span>
            </div>
            <template #overlay>
              <a-menu>
                <a-menu-item key="profile">
                  <user-outlined />
                  个人中心
                </a-menu-item>
                <a-menu-item key="password">
                  <lock-outlined />
                  修改密码
                </a-menu-item>
                <a-menu-divider />
                <a-menu-item key="logout" @click="handleLogout">
                  <logout-outlined />
                  退出登录
                </a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <a-layout-content class="layout-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useAppStore } from '@/stores/app'
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  UserOutlined,
  LockOutlined,
  LogoutOutlined,
  DashboardOutlined,
  SettingOutlined,
  TeamOutlined,
  MenuOutlined as MenuIconOutlined
} from '@ant-design/icons-vue'
import { Modal } from 'ant-design-vue'
import type { MenuProps } from 'ant-design-vue'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const appStore = useAppStore()

const selectedKeys = ref<string[]>([])
const openKeys = ref<string[]>([])

// 图标映射（暂时不使用，保留以备后续动态菜单使用）
// const iconMap: Record<string, any> = {
//   DashboardOutlined,
//   SettingOutlined,
//   UserOutlined,
//   TeamOutlined,
//   MenuOutlined: MenuIconOutlined
// }

// 菜单项
const menuItems = computed<MenuProps['items']>(() => {
  return [
    {
      key: '/dashboard',
      icon: () => h(DashboardOutlined),
      label: '仪表盘',
      title: '仪表盘'
    },
    {
      key: '/system',
      icon: () => h(SettingOutlined),
      label: '系统管理',
      title: '系统管理',
      children: [
        {
          key: '/system/user',
          icon: () => h(UserOutlined),
          label: '用户管理',
          title: '用户管理'
        },
        {
          key: '/system/role',
          icon: () => h(TeamOutlined),
          label: '角色管理',
          title: '角色管理'
        },
        {
          key: '/system/menu',
          icon: () => h(MenuIconOutlined),
          label: '菜单管理',
          title: '菜单管理'
        }
      ]
    }
  ]
})

// 监听路由变化
watch(
  () => route.path,
  (path) => {
    selectedKeys.value = [path]
    // 自动展开父级菜单
    const pathArray = path.split('/').filter(Boolean)
    if (pathArray.length > 1) {
      openKeys.value = [`/${pathArray[0]}`]
    }
  },
  { immediate: true }
)

// 菜单点击
function handleMenuClick({ key }: { key: string }) {
  router.push(key)
}

// 退出登录
function handleLogout() {
  Modal.confirm({
    title: '确认退出',
    content: '确定要退出登录吗？',
    onOk: () => {
      userStore.logout()
      router.push('/login')
    }
  })
}

// 需要导入 h 函数
import { h } from 'vue'
</script>

<style scoped>
.basic-layout {
  min-height: 100vh;
}

.layout-sider {
  box-shadow: 2px 0 8px 0 rgba(29, 35, 41, 0.05);
}

.logo {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.1);
  margin: 16px;
  border-radius: 8px;
}

.logo-text {
  color: #fff;
  font-size: 20px;
  font-weight: bold;
}

.logo-text-mini {
  color: #fff;
  font-size: 18px;
  font-weight: bold;
}

.layout-header {
  background: #fff;
  padding: 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
}

.header-left {
  display: flex;
  align-items: center;
}

.trigger {
  font-size: 18px;
  cursor: pointer;
  transition: color 0.3s;
}

.trigger:hover {
  color: #1890ff;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
  transition: background-color 0.3s;
}

.user-info:hover {
  background-color: rgba(0, 0, 0, 0.025);
}

.user-name {
  font-size: 14px;
}

.layout-content {
  margin: 24px;
  padding: 24px;
  background: #fff;
  border-radius: 8px;
  min-height: calc(100vh - 112px);
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
