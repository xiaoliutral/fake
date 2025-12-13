<template>
  <div class="dashboard">
    <a-row :gutter="16">
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card class="stat-card">
          <a-statistic
            title="用户总数"
            :value="statistics.userCount"
            :prefix="h(UserOutlined)"
            :value-style="{ color: '#3f8600' }"
          />
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card class="stat-card">
          <a-statistic
            title="角色总数"
            :value="statistics.roleCount"
            :prefix="h(TeamOutlined)"
            :value-style="{ color: '#cf1322' }"
          />
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card class="stat-card">
          <a-statistic
            title="菜单总数"
            :value="statistics.menuCount"
            :prefix="h(MenuOutlined)"
            :value-style="{ color: '#1890ff' }"
          />
        </a-card>
      </a-col>
      <a-col :xs="24" :sm="12" :lg="6">
        <a-card class="stat-card">
          <a-statistic
            title="在线用户"
            :value="statistics.onlineCount"
            :prefix="h(GlobalOutlined)"
            :value-style="{ color: '#722ed1' }"
          />
        </a-card>
      </a-col>
    </a-row>

    <a-row :gutter="16" style="margin-top: 16px">
      <a-col :xs="24" :lg="16">
        <a-card title="欢迎使用" :bordered="false">
          <a-descriptions :column="1">
            <a-descriptions-item label="用户名">
              {{ userStore.userInfo?.name }}
            </a-descriptions-item>
            <a-descriptions-item label="账号">
              {{ userStore.userInfo?.account }}
            </a-descriptions-item>
            <a-descriptions-item label="邮箱">
              {{ userStore.userInfo?.email || '-' }}
            </a-descriptions-item>
            <a-descriptions-item label="角色">
              <a-tag
                v-for="role in userStore.userInfo?.roles"
                :key="role.id"
                color="blue"
              >
                {{ role.name }}
              </a-tag>
            </a-descriptions-item>
            <a-descriptions-item label="权限数量">
              {{ userStore.permissions.length }}
            </a-descriptions-item>
          </a-descriptions>
        </a-card>
      </a-col>

      <a-col :xs="24" :lg="8">
        <a-card title="快速操作" :bordered="false">
          <a-space direction="vertical" style="width: 100%">
            <a-button type="primary" block @click="router.push('/system/user')">
              <user-outlined />
              用户管理
            </a-button>
            <a-button block @click="router.push('/system/role')">
              <team-outlined />
              角色管理
            </a-button>
            <a-button block @click="router.push('/system/menu')">
              <menu-outlined />
              菜单管理
            </a-button>
          </a-space>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup lang="ts">
import { ref, h } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import {
  UserOutlined,
  TeamOutlined,
  MenuOutlined,
  GlobalOutlined
} from '@ant-design/icons-vue'

const router = useRouter()
const userStore = useUserStore()

const statistics = ref({
  userCount: 0,
  roleCount: 0,
  menuCount: 0,
  onlineCount: 1
})
</script>

<style scoped>
.dashboard {
  padding: 24px;
}

.stat-card {
  margin-bottom: 16px;
}

:deep(.ant-statistic-title) {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.65);
}

:deep(.ant-statistic-content) {
  font-size: 24px;
  font-weight: 600;
}
</style>
