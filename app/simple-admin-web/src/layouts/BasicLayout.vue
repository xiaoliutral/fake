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
          <a-tooltip title="刷新菜单">
            <reload-outlined class="header-action" @click="handleRefreshMenu" />
          </a-tooltip>
          <a-dropdown>
            <div class="user-info">
              <a-avatar :size="32" :src="userStore.userInfo?.avatar">
                <template #icon><user-outlined /></template>
              </a-avatar>
              <span class="user-name">{{ userStore.userInfo?.name }}</span>
            </div>
            <template #overlay>
              <a-menu @click="handleUserMenuClick">
                <a-menu-item key="profile">
                  <user-outlined />
                  个人中心
                </a-menu-item>
                <a-menu-divider />
                <a-menu-item key="logout">
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
import { ref, computed, watch, h, type Component as VueComponent } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useAppStore } from '@/stores/app'
import type { MenuTreeDto } from '@/api'
import * as Icons from '@ant-design/icons-vue'
import { Modal, message } from 'ant-design-vue'
import type { ItemType } from 'ant-design-vue'

// 单独导入常用图标用于模板
const {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  UserOutlined,
  LogoutOutlined,
  DashboardOutlined,
  ReloadOutlined
} = Icons

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const appStore = useAppStore()

const selectedKeys = ref<string[]>([])
const openKeys = ref<string[]>([])

// 图标名称映射：后端使用小写名称，前端需要转换为 Ant Design 图标名称
const iconNameMap: Record<string, string> = {
  'setting': 'SettingOutlined',
  'user': 'UserOutlined',
  'team': 'TeamOutlined',
  'menu': 'MenuOutlined',
  'apartment': 'ApartmentOutlined',
  'dashboard': 'DashboardOutlined',
  'home': 'HomeOutlined',
  'file': 'FileOutlined',
  'folder': 'FolderOutlined',
  'database': 'DatabaseOutlined',
  'cloud': 'CloudOutlined',
  'api': 'ApiOutlined',
  'bell': 'BellOutlined',
  'calendar': 'CalendarOutlined',
  'profile': 'ProfileOutlined',
  'table': 'TableOutlined',
  'form': 'FormOutlined',
  'pie-chart': 'PieChartOutlined',
  'bar-chart': 'BarChartOutlined',
  'line-chart': 'LineChartOutlined',
  'shop': 'ShopOutlined',
  'shopping-cart': 'ShoppingCartOutlined',
  'gift': 'GiftOutlined',
  'wallet': 'WalletOutlined',
  'bank': 'BankOutlined',
  'audit': 'AuditOutlined',
  'solution': 'SolutionOutlined',
  'idcard': 'IdcardOutlined',
  'contacts': 'ContactsOutlined',
  'schedule': 'ScheduleOutlined',
  'file-text': 'FileTextOutlined',
  'cluster': 'ClusterOutlined',
  'global': 'GlobalOutlined',
  'environment': 'EnvironmentOutlined',
  'rocket': 'RocketOutlined',
  'experiment': 'ExperimentOutlined',
  'bug': 'BugOutlined',
  'code': 'CodeOutlined',
  'build': 'BuildOutlined',
  'block': 'BlockOutlined',
  'layout': 'LayoutOutlined',
  'appstore': 'AppstoreOutlined',
  'key': 'KeyOutlined',
  'lock': 'LockOutlined',
  'eye': 'EyeOutlined',
  'search': 'SearchOutlined',
  'edit': 'EditOutlined',
  'delete': 'DeleteOutlined',
  'save': 'SaveOutlined',
  'copy': 'CopyOutlined',
  'upload': 'UploadOutlined',
  'download': 'DownloadOutlined',
  'printer': 'PrinterOutlined',
  'mail': 'MailOutlined',
  'message': 'MessageOutlined',
  'notification': 'NotificationOutlined',
  'phone': 'PhoneOutlined',
  'mobile': 'MobileOutlined',
  'desktop': 'DesktopOutlined',
  'laptop': 'LaptopOutlined',
  'camera': 'CameraOutlined',
  'picture': 'PictureOutlined',
  'link': 'LinkOutlined',
  'qrcode': 'QrcodeOutlined',
  'tag': 'TagOutlined',
  'tags': 'TagsOutlined',
  'book': 'BookOutlined',
  'read': 'ReadOutlined',
  'star': 'StarOutlined',
  'heart': 'HeartOutlined',
  'smile': 'SmileOutlined',
  'like': 'LikeOutlined',
  'user-add': 'UserAddOutlined',
  'github': 'GithubOutlined',
  'google': 'GoogleOutlined',
  'wechat': 'WechatOutlined',
  'alipay': 'AlipayOutlined',
  'safety': 'SafetyOutlined',
  'tool': 'ToolOutlined',
  'fund': 'FundOutlined',
  'money-collect': 'MoneyCollectOutlined',
  'insurance': 'InsuranceOutlined',
  'carry-out': 'CarryOutOutlined',
  'reconciliation': 'ReconciliationOutlined',
  'file-done': 'FileDoneOutlined',
  'file-search': 'FileSearchOutlined',
  'folder-open': 'FolderOpenOutlined',
  'folder-add': 'FolderAddOutlined',
  'deployment-unit': 'DeploymentUnitOutlined',
  'compass': 'CompassOutlined',
  'aim': 'AimOutlined',
  'send': 'SendOutlined',
  'thunderbolt': 'ThunderboltOutlined',
  'fire': 'FireOutlined',
  'console-sql': 'ConsoleSqlOutlined',
  'partition': 'PartitionOutlined',
  'group': 'GroupOutlined',
  'swap': 'SwapOutlined',
  'sync': 'SyncOutlined',
  'reload': 'ReloadOutlined',
  'poweroff': 'PoweroffOutlined',
  'login': 'LoginOutlined',
  'logout': 'LogoutOutlined',
  'unlock': 'UnlockOutlined',
  'plus': 'PlusOutlined',
  'minus': 'MinusOutlined',
  'close': 'CloseOutlined',
  'check': 'CheckOutlined',
  'info': 'InfoOutlined',
  'question': 'QuestionOutlined',
  'warning': 'WarningOutlined',
  'stop': 'StopOutlined',
  'pause': 'PauseOutlined'
}

// 获取图标组件
function getIconComponent(iconName: string | null | undefined): VueComponent | null {
  if (!iconName) return null
  
  // 先尝试从映射表获取
  let mappedName = iconNameMap[iconName.toLowerCase()]
  
  // 如果映射表没有，尝试直接使用（可能已经是完整名称）
  if (!mappedName) {
    // 如果已经包含 Outlined/Filled/TwoTone 后缀，直接使用
    if (iconName.includes('Outlined') || iconName.includes('Filled') || iconName.includes('TwoTone')) {
      mappedName = iconName
    } else {
      // 尝试转换为 PascalCase + Outlined
      mappedName = iconName.charAt(0).toUpperCase() + iconName.slice(1) + 'Outlined'
    }
  }
  
  // 从 Icons 对象中获取图标组件
  const icon = (Icons as Record<string, VueComponent>)[mappedName]
  return icon || null
}

// 静态菜单（首页等固定菜单）
const staticMenus: ItemType[] = [
  {
    key: '/dashboard',
    icon: () => h(DashboardOutlined),
    label: '首页',
    title: '首页'
  }
]

// 将后端菜单数据转换为 Ant Design Menu 格式
function convertMenuToAntd(menu: MenuTreeDto): ItemType {
  const iconComponent = getIconComponent(menu.icon)
  
  // 使用 route 作为 key，如果没有 route 则使用 id
  const menuItem: any = {
    key: menu.route || menu.id,
    label: menu.name,
    title: menu.name
  }
  
  if (iconComponent) {
    menuItem.icon = () => h(iconComponent)
  }
  
  // 如果有子菜单且不是按钮类型
  if (menu.children && menu.children.length > 0 && menu.type !== 2) {
    const visibleChildren = menu.children.filter(child => !child.isHidden && child.type !== 2)
    if (visibleChildren.length > 0) {
      menuItem.children = visibleChildren.map(child => convertMenuToAntd(child))
    }
  }
  
  return menuItem as ItemType
}

// 动态菜单项
const menuItems = computed<ItemType[]>(() => {
  const backendMenus = userStore.menus
  
  // 始终包含静态菜单（首页）
  const items: ItemType[] = [...staticMenus]
  
  if (backendMenus && backendMenus.length > 0) {
    // 过滤掉隐藏的菜单和按钮类型
    const visibleMenus = backendMenus.filter(menu => !menu.isHidden && menu.type !== 2)
    items.push(...visibleMenus.map(menu => convertMenuToAntd(menu)))
  }
  
  return items
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

// 用户菜单点击
function handleUserMenuClick({ key }: { key: string }) {
  if (key === 'profile') {
    router.push('/profile')
  } else if (key === 'logout') {
    handleLogout()
  }
}

// 刷新菜单
async function handleRefreshMenu() {
  try {
    await userStore.refreshUserInfo()
    message.success('菜单已刷新')
  } catch {
    message.error('刷新失败')
  }
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

.header-action {
  font-size: 16px;
  cursor: pointer;
  padding: 8px;
  border-radius: 4px;
  transition: background-color 0.3s;
}

.header-action:hover {
  background-color: rgba(0, 0, 0, 0.025);
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
