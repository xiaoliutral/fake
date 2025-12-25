<template>
  <div class="profile-page">
    <a-row :gutter="24">
      <!-- 左侧：用户信息卡片 -->
      <a-col :span="8">
        <a-card title="个人信息" :bordered="false">
          <div class="user-info">
            <div class="avatar-section">
              <a-avatar :size="100" :src="getAvatarUrl(userInfo?.avatar)">
                <template #icon><user-outlined /></template>
              </a-avatar>
              <a-upload
                :show-upload-list="false"
                :before-upload="handleAvatarUpload"
                accept="image/*"
              >
                <a-button type="link" :loading="avatarLoading">
                  <upload-outlined /> 上传头像
                </a-button>
              </a-upload>
            </div>
            <a-descriptions :column="1" class="info-descriptions">
              <a-descriptions-item label="用户名">
                <span v-if="!editingName">{{ userInfo?.name }}</span>
                <a-input
                  v-else
                  v-model:value="editName"
                  size="small"
                  style="width: 120px"
                />
                <a-button
                  v-if="!editingName"
                  type="link"
                  size="small"
                  @click="startEditName"
                >
                  <edit-outlined />
                </a-button>
                <template v-else>
                  <a-button
                    type="link"
                    size="small"
                    :loading="profileLoading"
                    @click="saveProfile"
                  >
                    <check-outlined />
                  </a-button>
                  <a-button type="link" size="small" @click="cancelEditName">
                    <close-outlined />
                  </a-button>
                </template>
              </a-descriptions-item>
              <a-descriptions-item label="账号">
                {{ userInfo?.account }}
              </a-descriptions-item>
              <a-descriptions-item label="邮箱">
                {{ userInfo?.email || '-' }}
              </a-descriptions-item>
              <a-descriptions-item label="角色">
                <a-tag v-for="role in userInfo?.roles" :key="role.id" color="blue">
                  {{ role.name }}
                </a-tag>
              </a-descriptions-item>
            </a-descriptions>
          </div>
        </a-card>
      </a-col>

      <!-- 右侧：修改密码 -->
      <a-col :span="16">
        <a-card title="修改密码" :bordered="false">
          <a-form
            ref="passwordFormRef"
            :model="passwordForm"
            :rules="passwordRules"
            :label-col="{ span: 6 }"
            :wrapper-col="{ span: 14 }"
          >
            <a-form-item label="当前密码" name="oldPassword">
              <a-input-password
                v-model:value="passwordForm.oldPassword"
                placeholder="请输入当前密码"
              />
            </a-form-item>
            <a-form-item label="新密码" name="newPassword">
              <a-input-password
                v-model:value="passwordForm.newPassword"
                placeholder="请输入新密码"
              />
            </a-form-item>
            <a-form-item label="确认密码" name="confirmPassword">
              <a-input-password
                v-model:value="passwordForm.confirmPassword"
                placeholder="请再次输入新密码"
              />
            </a-form-item>
            <a-form-item :wrapper-col="{ offset: 6, span: 14 }">
              <a-button
                type="primary"
                :loading="passwordLoading"
                @click="handleChangePassword"
              >
                修改密码
              </a-button>
            </a-form-item>
          </a-form>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue'
import { message } from 'ant-design-vue'
import { useUserStore } from '@/stores/user'
import { AuthService } from '@/api'
import { handleApiError } from '@/utils/error-handler'
import {
  UserOutlined,
  UploadOutlined,
  EditOutlined,
  CheckOutlined,
  CloseOutlined
} from '@ant-design/icons-vue'
import type { Rule } from 'ant-design-vue/es/form'
import type { UploadProps } from 'ant-design-vue'

const userStore = useUserStore()
const userInfo = computed(() => userStore.userInfo)

// 获取头像完整URL
function getAvatarUrl(avatar: string | null | undefined): string | undefined {
  if (!avatar) return undefined
  if (avatar.startsWith('http')) return avatar
  // 相对路径，通过 vite 代理访问
  return avatar
}

// 用户名编辑
const editingName = ref(false)
const editName = ref('')
const profileLoading = ref(false)

function startEditName() {
  editName.value = userInfo.value?.name || ''
  editingName.value = true
}

function cancelEditName() {
  editingName.value = false
  editName.value = ''
}

async function saveProfile() {
  if (!editName.value.trim()) {
    message.warning('用户名不能为空')
    return
  }

  profileLoading.value = true
  try {
    await AuthService.putRbacAuthUpdateProfile({
      name: editName.value.trim(),
      email: undefined
    })
    message.success('用户名修改成功')
    editingName.value = false
    // 刷新用户信息
    await userStore.getCurrentUser()
  } catch (error) {
    handleApiError(error)
  } finally {
    profileLoading.value = false
  }
}

// 头像上传
const avatarLoading = ref(false)

const handleAvatarUpload: UploadProps['beforeUpload'] = async (file) => {
  // 验证文件类型
  const isImage = file.type.startsWith('image/')
  if (!isImage) {
    message.error('只能上传图片文件')
    return false
  }

  // 验证文件大小（10MB）
  const isLt10M = file.size / 1024 / 1024 < 10
  if (!isLt10M) {
    message.error('图片大小不能超过10MB')
    return false
  }

  avatarLoading.value = true
  try {
    await AuthService.postRbacAuthUploadAvatar({
      formData: { file }
    })
    message.success('头像上传成功')

    // 刷新用户信息
    await userStore.getCurrentUser()
  } catch (error: any) {
    handleApiError(error)
  } finally {
    avatarLoading.value = false
  }

  return false // 阻止默认上传行为
}

// 密码表单
const passwordFormRef = ref()
const passwordLoading = ref(false)
const passwordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const validateConfirmPassword = async (_rule: Rule, value: string) => {
  if (value !== passwordForm.newPassword) {
    return Promise.reject('两次输入的密码不一致')
  }
  return Promise.resolve()
}

const passwordRules: Record<string, Rule[]> = {
  oldPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

async function handleChangePassword() {
  try {
    await passwordFormRef.value.validate()
    passwordLoading.value = true

    await AuthService.postRbacAuthChangePassword({
      oldPassword: passwordForm.oldPassword,
      newPassword: passwordForm.newPassword
    })

    message.success('密码修改成功，请重新登录')

    // 清空表单
    passwordForm.oldPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmPassword = ''

    // 登出并跳转到登录页
    setTimeout(() => {
      userStore.logout()
      window.location.href = '/login'
    }, 1500)
  } catch (error: any) {
    if (error.errorFields) {
      return
    }
    handleApiError(error)
  } finally {
    passwordLoading.value = false
  }
}
</script>

<style scoped>
.profile-page {
  padding: 24px;
}

.user-info {
  text-align: center;
}

.avatar-section {
  margin-bottom: 24px;
}

.avatar-section .ant-avatar {
  margin-bottom: 12px;
}

.info-descriptions {
  text-align: left;
  margin-top: 16px;
}

.info-descriptions :deep(.ant-descriptions-item-label) {
  width: 80px;
}

.info-descriptions :deep(.ant-descriptions-item-content) {
  display: flex;
  align-items: center;
  gap: 4px;
}
</style>
