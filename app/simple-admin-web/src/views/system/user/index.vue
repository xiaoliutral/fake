<template>
  <div class="user-page">
    <div class="page-header">
      <a-space>
        <a-button
          v-if="hasPermission('Rbac.Users.Create')"
          type="primary"
          @click="handleAdd"
        >
          <template #icon><plus-outlined /></template>
          新增用户
        </a-button>
        <a-button
          v-if="hasPermission('Rbac.Users.Delete')"
          danger
          :disabled="!selectedRowKeys.length"
          @click="handleBatchDelete"
        >
          <template #icon><delete-outlined /></template>
          批量删除
        </a-button>
      </a-space>

      <a-space>
        <a-tree-select
          v-model:value="filterOrganizationId"
          placeholder="选择组织筛选"
          style="width: 180px"
          :tree-data="organizationTree"
          :field-names="{ label: 'name', value: 'id', children: 'children' }"
          allow-clear
          @change="handleSearch"
        />
        <a-input-search
          v-model:value="searchKeyword"
          placeholder="搜索用户名或账号"
          style="width: 250px"
          @search="handleSearch"
        />
        <a-button @click="loadData">
          <template #icon><reload-outlined /></template>
        </a-button>
      </a-space>
    </div>

    <a-table
      :columns="columns"
      :data-source="dataSource"
      :loading="loading"
      :pagination="pagination"
      :row-selection="rowSelection"
      :scroll="{ x: 1200 }"
      @change="handleTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'avatar'">
          <a-avatar :src="record.avatar">
            <template #icon><user-outlined /></template>
          </a-avatar>
        </template>

        <template v-else-if="column.key === 'organizationName'">
          {{ record.organizationName || '-' }}
        </template>

        <template v-else-if="column.key === 'roles'">
          <a-tag v-for="role in record.roles" :key="role.id" color="blue">
            {{ role.name }}
          </a-tag>
        </template>

        <template v-else-if="column.key === 'createdAt'">
          {{ dayjs(record.createdAt).format('YYYY-MM-DD HH:mm:ss') }}
        </template>

        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button
              v-if="hasPermission('Rbac.Users.Update')"
              type="link"
              size="small"
              @click="handleEdit(record)"
            >
              编辑
            </a-button>
            <a-button
              v-if="hasPermission('Rbac.Users.AssignRoles')"
              type="link"
              size="small"
              @click="handleAssignRoles(record)"
            >
              分配角色
            </a-button>
            <a-popconfirm
              v-if="hasPermission('Rbac.Users.Delete')"
              title="确定要删除吗？"
              @confirm="handleDelete(record.id)"
            >
              <a-button type="link" size="small" danger>
                删除
              </a-button>
            </a-popconfirm>
          </a-space>
        </template>
      </template>
    </a-table>

    <!-- 新增/编辑用户弹窗 -->
    <a-modal
      v-model:open="modalVisible"
      :title="modalTitle"
      :confirm-loading="modalLoading"
      @ok="handleModalOk"
      width="600px"
    >
      <a-form
        ref="formRef"
        :model="formState"
        :rules="formRules"
        :label-col="{ span: 6 }"
        :wrapper-col="{ span: 16 }"
      >
        <a-form-item label="用户名" name="name">
          <a-input v-model:value="formState.name" placeholder="请输入用户名" />
        </a-form-item>

        <a-form-item label="账号" name="account">
          <a-input
            v-model:value="formState.account"
            placeholder="请输入账号"
            :disabled="isEdit"
          />
        </a-form-item>

        <a-form-item v-if="!isEdit" label="密码" name="password">
          <a-input-password
            v-model:value="formState.password"
            placeholder="请输入密码"
          />
        </a-form-item>

        <a-form-item label="邮箱" name="email">
          <a-input v-model:value="formState.email" placeholder="请输入邮箱" />
        </a-form-item>

        <a-form-item label="所属组织" name="organizationId">
          <a-tree-select
            v-model:value="formState.organizationId"
            placeholder="请选择所属组织"
            :tree-data="organizationTree"
            :field-names="{ label: 'name', value: 'id', children: 'children' }"
            allow-clear
          />
        </a-form-item>

        <a-form-item label="头像" name="avatar">
          <a-input v-model:value="formState.avatar" placeholder="请输入头像URL" />
        </a-form-item>

        <a-form-item v-if="!isEdit" label="角色" name="roleIds">
          <a-select
            v-model:value="formState.roleIds"
            mode="multiple"
            placeholder="请选择角色"
            :options="roleOptions"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 分配角色弹窗 -->
    <a-modal
      v-model:open="roleModalVisible"
      title="分配角色"
      :confirm-loading="roleModalLoading"
      @ok="handleRoleModalOk"
    >
      <a-checkbox-group v-model:value="selectedRoleIds" style="width: 100%">
        <a-row>
          <a-col
            v-for="role in allRoles"
            :key="role.id"
            :span="24"
            style="margin-bottom: 8px"
          >
            <a-checkbox :value="role.id">{{ role.name }}</a-checkbox>
          </a-col>
        </a-row>
      </a-checkbox-group>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { UserService, RoleService, OrganizationService } from '@/api'
import type { UserDto, UserCreateDto, UserUpdateDto, RoleSimpleDto, OrganizationTreeDto } from '@/api'
import { hasPermission } from '@/utils/permission'
import { handleApiError } from '@/utils/error-handler'
import {
  PlusOutlined,
  DeleteOutlined,
  ReloadOutlined,
  UserOutlined
} from '@ant-design/icons-vue'
import dayjs from 'dayjs'
import type { TableColumnsType } from 'ant-design-vue'

const loading = ref(false)
const dataSource = ref<UserDto[]>([])
const searchKeyword = ref('')
const filterOrganizationId = ref<string>()
const selectedRowKeys = ref<string[]>([])
const organizationTree = ref<OrganizationTreeDto[]>([])

const pagination = reactive({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  showQuickJumper: true,
  showTotal: (total: number) => `共 ${total} 条`
})

const columns: TableColumnsType = [
  { title: '头像', key: 'avatar', width: 80, fixed: 'left' },
  { title: '用户名', dataIndex: 'name', key: 'name', width: 120 },
  { title: '账号', dataIndex: 'account', key: 'account', width: 120 },
  { title: '邮箱', dataIndex: 'email', key: 'email', width: 180 },
  { title: '所属组织', key: 'organizationName', width: 150 },
  { title: '角色', key: 'roles', width: 200 },
  { title: '创建时间', key: 'createdAt', width: 180 },
  { title: '操作', key: 'action', width: 250, fixed: 'right' }
]

const rowSelection = computed(() => ({
  selectedRowKeys: selectedRowKeys.value,
  onChange: (keys: string[]) => {
    selectedRowKeys.value = keys
  }
}))

// 表单相关
const modalVisible = ref(false)
const modalLoading = ref(false)
const modalTitle = ref('新增用户')
const isEdit = ref(false)
const formRef = ref()
const currentId = ref('')

const formState = reactive<UserCreateDto & { id?: string }>({
  name: '',
  account: '',
  password: '',
  email: '',
  avatar: '',
  organizationId: undefined,
  roleIds: []
})

const formRules = {
  name: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  account: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6位', trigger: 'blur' }
  ],
  email: [{ type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }]
}

// 角色相关
const allRoles = ref<RoleSimpleDto[]>([])
const roleOptions = computed(() =>
  allRoles.value.map(r => ({ label: r.name, value: r.id }))
)

// 分配角色弹窗
const roleModalVisible = ref(false)
const roleModalLoading = ref(false)
const selectedRoleIds = ref<string[]>([])
const currentUserId = ref('')

onMounted(() => {
  loadData()
  loadRoles()
  loadOrganizations()
})

async function loadData() {
  loading.value = true
  try {
    const result = await UserService.getRbacUserGetList({
      keyword: searchKeyword.value || undefined,
      organizationId: filterOrganizationId.value || undefined,
      page: pagination.current,
      pageSize: pagination.pageSize
    })
    dataSource.value = result.items || []
    pagination.total = result.total || 0
  } catch (error) {
    handleApiError(error)
  } finally {
    loading.value = false
  }
}

async function loadRoles() {
  try {
    allRoles.value = await RoleService.getRbacRoleGetAllRoles()
  } catch (error) {
    handleApiError(error)
  }
}

async function loadOrganizations() {
  try {
    organizationTree.value = await OrganizationService.getRbacOrganizationGetTree()
  } catch (error) {
    handleApiError(error)
  }
}

function handleSearch() {
  pagination.current = 1
  loadData()
}

function handleTableChange(pag: any) {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  loadData()
}

function handleAdd() {
  modalTitle.value = '新增用户'
  isEdit.value = false
  Object.assign(formState, {
    name: '',
    account: '',
    password: '',
    email: '',
    avatar: '',
    organizationId: undefined,
    roleIds: []
  })
  modalVisible.value = true
}

function handleEdit(record: UserDto) {
  modalTitle.value = '编辑用户'
  isEdit.value = true
  currentId.value = record.id || ''
  Object.assign(formState, {
    name: record.name,
    account: record.account,
    email: record.email,
    avatar: record.avatar,
    organizationId: record.organizationId
  })
  modalVisible.value = true
}

async function handleModalOk() {
  try {
    await formRef.value.validate()
    modalLoading.value = true

    if (isEdit.value) {
      const updateData: UserUpdateDto = {
        name: formState.name,
        email: formState.email,
        avatar: formState.avatar,
        organizationId: formState.organizationId
      }
      await UserService.putRbacUserUpdate({ id: currentId.value, requestBody: updateData })
      message.success('更新成功')
    } else {
      await UserService.postRbacUserCreate({ requestBody: formState })
      message.success('创建成功')
    }

    modalVisible.value = false
    loadData()
  } catch (error: any) {
    if (error.errorFields) {
      return
    }
    handleApiError(error)
  } finally {
    modalLoading.value = false
  }
}

async function handleDelete(id: string) {
  try {
    await UserService.deleteRbacUserDelete({ id })
    message.success('删除成功')
    loadData()
  } catch (error) {
    handleApiError(error)
  }
}

function handleBatchDelete() {
  Modal.confirm({
    title: '确认删除',
    content: `确定要删除选中的 ${selectedRowKeys.value.length} 个用户吗？`,
    onOk: async () => {
      try {
        await UserService.deleteRbacUserDeleteBatch({ ids: selectedRowKeys.value as string[] })
        message.success('删除成功')
        selectedRowKeys.value = []
        loadData()
      } catch (error) {
        handleApiError(error)
      }
    }
  })
}

async function handleAssignRoles(record: UserDto) {
  currentUserId.value = record.id!
  try {
    const roles = await UserService.getRbacUserGetUserRoles({ userId: record.id })
    selectedRoleIds.value = roles.map((r: any) => r.id!)
    roleModalVisible.value = true
  } catch (error) {
    handleApiError(error)
  }
}

async function handleRoleModalOk() {
  roleModalLoading.value = true
  try {
    await UserService.postRbacUserAssignRoles({
      userId: currentUserId.value,
      requestBody: selectedRoleIds.value
    })
    message.success('分配角色成功')
    roleModalVisible.value = false
    loadData()
  } catch (error) {
    handleApiError(error)
  } finally {
    roleModalLoading.value = false
  }
}
</script>

<style scoped>
.user-page {
  padding: 24px;
  background: #fff;
  border-radius: 8px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}
</style>
