<template>
  <div class="role-page">
    <div class="page-header">
      <a-space>
        <a-button
          v-if="hasPermission('Rbac.Roles.Create')"
          type="primary"
          @click="handleAdd"
        >
          <template #icon><plus-outlined /></template>
          新增角色
        </a-button>
      </a-space>

      <a-space>
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
      @change="handleTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'createdAt'">
          {{ dayjs(record.createdAt).format('YYYY-MM-DD HH:mm:ss') }}
        </template>

        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button
              v-if="hasPermission('Rbac.Roles.Update')"
              type="link"
              size="small"
              @click="handleEdit(record)"
            >
              编辑
            </a-button>
            <a-button
              v-if="hasPermission('Rbac.Roles.AssignPermissions')"
              type="link"
              size="small"
              @click="handleAssignPermissions(record)"
            >
              分配权限
            </a-button>
            <a-popconfirm
              v-if="hasPermission('Rbac.Roles.Delete')"
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

    <!-- 新增/编辑角色弹窗 -->
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
        <a-form-item label="角色名称" name="name">
          <a-input v-model:value="formState.name" placeholder="请输入角色名称" />
        </a-form-item>

        <a-form-item label="角色编码" name="code">
          <a-input
            v-model:value="formState.code"
            placeholder="请输入角色编码"
            :disabled="isEdit"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 分配权限弹窗 -->
    <a-modal
      v-model:open="permissionModalVisible"
      title="分配权限"
      :confirm-loading="permissionModalLoading"
      @ok="handlePermissionModalOk"
      width="800px"
    >
      <div class="permission-tree-container">
        <div class="permission-actions">
          <a-button size="small" @click="expandAllPermissions">全部展开</a-button>
          <a-button size="small" @click="collapseAllPermissions">全部收起</a-button>
          <a-button size="small" @click="checkAllPermissions">全选</a-button>
          <a-button size="small" @click="uncheckAllPermissions">取消全选</a-button>
        </div>
        <a-tree
          v-model:checkedKeys="selectedPermissions"
          v-model:expandedKeys="permissionExpandedKeys"
          checkable
          :tree-data="permissionTreeData"
          :field-names="{ title: 'name', key: 'code', children: 'children' }"
          check-strictly
          :height="400"
          virtual
        />
      </div>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { RoleService, PermissionService } from '@/api'
import type { RoleDto, RoleCreateDto, RoleUpdateDto, PermissionGroupDto } from '@/api'
import { hasPermission } from '@/utils/permission'
import { handleApiError } from '@/utils/error-handler'
import {
  PlusOutlined,
  ReloadOutlined
} from '@ant-design/icons-vue'
import dayjs from 'dayjs'
import type { TableColumnsType } from 'ant-design-vue'

const loading = ref(false)
const dataSource = ref<RoleDto[]>([])

const pagination = reactive({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  showQuickJumper: true,
  showTotal: (total: number) => `共 ${total} 条`
})

const columns: TableColumnsType = [
  { title: '角色名称', dataIndex: 'name', key: 'name', width: 150 },
  { title: '角色编码', dataIndex: 'code', key: 'code', width: 150 },
  { title: '创建时间', key: 'createdAt', width: 180 },
  { title: '操作', key: 'action', width: 250, fixed: 'right' }
]

// 表单相关
const modalVisible = ref(false)
const modalLoading = ref(false)
const modalTitle = ref('新增角色')
const isEdit = ref(false)
const formRef = ref()
const currentId = ref('')

const formState = reactive<RoleCreateDto>({
  name: '',
  code: ''
})

const formRules = {
  name: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入角色编码', trigger: 'blur' }]
}

// 权限树相关
const permissionTree = ref<PermissionGroupDto[]>([])
const selectedPermissions = ref<string[]>([])
const permissionExpandedKeys = ref<string[]>([])
const permissionModalVisible = ref(false)
const permissionModalLoading = ref(false)
const currentRoleId = ref('')

const permissionTreeData = computed(() => permissionTree.value)

onMounted(() => {
  loadData()
  loadPermissions()
})

async function loadData() {
  loading.value = true
  try {
    const result = await RoleService.getRbacRoleGetList(
      undefined, // keyword
      pagination.current,
      pagination.pageSize
    )
    dataSource.value = result.items || []
    pagination.total = result.total || 0
  } catch (error) {
    handleApiError(error)
  } finally {
    loading.value = false
  }
}

async function loadPermissions() {
  try {
    const groups = await PermissionService.getRbacPermissionGetPermissionTree()
    // 将分组转换为树形结构（使用 permissions 作为第一级子节点）
    permissionTree.value = groups.map(group => ({
      ...group,
      children: group.permissions || []
    }))
    // 默认展开前两级
    permissionExpandedKeys.value = getPermissionExpandedKeys(permissionTree.value, 2)
  } catch (error) {
    handleApiError(error)
  }
}

// 获取权限树默认展开的节点（递归处理 children）
function getPermissionExpandedKeys(data: any[], maxLevel: number, currentLevel = 1): string[] {
  const keys: string[] = []
  if (currentLevel > maxLevel) return keys
  
  data.forEach(item => {
    if (item.code) {
      keys.push(item.code)
    }
    if (item.children && item.children.length > 0) {
      keys.push(...getPermissionExpandedKeys(item.children, maxLevel, currentLevel + 1))
    }
  })
  return keys
}

function handleTableChange(pag: any) {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  loadData()
}

function handleAdd() {
  modalTitle.value = '新增角色'
  isEdit.value = false
  Object.assign(formState, {
    name: '',
    code: ''
  })
  modalVisible.value = true
}

function handleEdit(record: RoleDto) {
  modalTitle.value = '编辑角色'
  isEdit.value = true
  currentId.value = record.id!
  Object.assign(formState, {
    name: record.name,
    code: record.code
  })
  modalVisible.value = true
}

async function handleModalOk() {
  try {
    await formRef.value.validate()
    modalLoading.value = true

    if (isEdit.value) {
      const updateData: RoleUpdateDto = {
        name: formState.name
      }
      await RoleService.putRbacRoleUpdate(currentId.value, updateData)
      message.success('更新成功')
    } else {
      await RoleService.postRbacRoleCreate(formState)
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
    await RoleService.deleteRbacRoleDelete(id)
    message.success('删除成功')
    loadData()
  } catch (error) {
    handleApiError(error)
  }
}

async function handleAssignPermissions(record: RoleDto) {
  currentRoleId.value = record.id!
  try {
    const permissions = await RoleService.getRbacRoleGetRolePermissions(record.id)
    selectedPermissions.value = permissions
    permissionModalVisible.value = true
  } catch (error) {
    handleApiError(error)
  }
}

async function handlePermissionModalOk() {
  permissionModalLoading.value = true
  try {
    await RoleService.postRbacRoleAssignPermissions(
      currentRoleId.value,
      selectedPermissions.value
    )
    message.success('分配权限成功')
    permissionModalVisible.value = false
    loadData()
  } catch (error) {
    handleApiError(error)
  } finally {
    permissionModalLoading.value = false
  }
}

// 展开所有权限节点
function expandAllPermissions() {
  const getAllKeys = (data: any[]): string[] => {
    const keys: string[] = []
    data.forEach(item => {
      if (item.code) {
        keys.push(item.code)
      }
      if (item.children && item.children.length > 0) {
        keys.push(...getAllKeys(item.children))
      }
    })
    return keys
  }
  permissionExpandedKeys.value = getAllKeys(permissionTreeData.value)
}

// 收起所有权限节点
function collapseAllPermissions() {
  permissionExpandedKeys.value = []
}

// 全选权限
function checkAllPermissions() {
  const getAllKeys = (data: any[]): string[] => {
    const keys: string[] = []
    data.forEach(item => {
      if (item.code) {
        keys.push(item.code)
      }
      if (item.children && item.children.length > 0) {
        keys.push(...getAllKeys(item.children))
      }
    })
    return keys
  }
  selectedPermissions.value = getAllKeys(permissionTreeData.value)
}

// 取消全选
function uncheckAllPermissions() {
  selectedPermissions.value = []
}
</script>

<style scoped>
.role-page {
  padding: 24px;
  background: #fff;
  border-radius: 8px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}

.permission-tree-container {
  max-height: 500px;
  overflow: auto;
}

.permission-actions {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.permission-tree-container :deep(.ant-tree) {
  background: #fafafa;
  padding: 12px;
  border-radius: 4px;
}

.permission-tree-container :deep(.ant-tree-node-content-wrapper) {
  padding: 4px 8px;
}

.permission-tree-container :deep(.ant-tree-title) {
  font-size: 14px;
}
</style>
