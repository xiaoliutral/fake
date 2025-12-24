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
      width="900px"
    >
      <div class="permission-container">
        <div class="permission-actions">
          <a-button size="small" @click="checkAllPermissions">全选</a-button>
          <a-button size="small" @click="uncheckAllPermissions">取消全选</a-button>
        </div>
        
        <div class="permission-groups">
          <div v-for="(group, index) in permissionTree" :key="group.code ?? `group-${index}`" class="permission-group">
            <div class="group-header">
              <a-checkbox
                :checked="isGroupChecked(group)"
                :indeterminate="isGroupIndeterminate(group)"
                @change="(e: any) => handleGroupCheck(group, e.target.checked)"
              >
                <span class="group-name">{{ group.name }}</span>
              </a-checkbox>
            </div>
            <div class="group-permissions">
              <!-- 菜单权限一行一行展示 -->
              <div v-for="perm in group.permissions" :key="perm.code ?? ''" class="menu-permission-row">
                <div class="menu-permission">
                  <a-checkbox
                    :checked="isPermissionChecked(perm.code)"
                    :indeterminate="isMenuIndeterminate(perm)"
                    @change="(e: any) => handleMenuPermissionChange(perm, e.target.checked)"
                  >
                    {{ perm.name }}
                  </a-checkbox>
                </div>
                <!-- 按钮权限横向展示在菜单下 -->
                <div v-if="perm.children && perm.children.length > 0" class="button-permissions">
                  <a-checkbox
                    v-for="child in perm.children"
                    :key="child.code ?? ''"
                    :checked="isPermissionChecked(child.code)"
                    @change="(e: any) => handleButtonPermissionChange(child.code, e.target.checked)"
                    class="button-permission-item"
                  >
                    {{ child.name }}
                  </a-checkbox>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { RoleService, PermissionService } from '@/api'
import type { RoleDto, RoleCreateDto, RoleUpdateDto, PermissionGroupDto, PermissionDefinitionDto } from '@/api'
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
const permissionModalVisible = ref(false)
const permissionModalLoading = ref(false)
const currentRoleId = ref('')

onMounted(() => {
  loadData()
  loadPermissions()
})

async function loadData() {
  loading.value = true
  try {
    const result = await RoleService.getRbacRoleGetList({
      keyword: undefined,
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

async function loadPermissions() {
  try {
    const groups = await PermissionService.getRbacPermissionGetPermissionTree()
    permissionTree.value = groups
  } catch (error) {
    handleApiError(error)
  }
}

// 检查分组是否全选
function isGroupChecked(group: PermissionGroupDto): boolean {
  if (!group.permissions || group.permissions.length === 0) return false
  return getAllGroupPermissionCodes(group).every(code => selectedPermissions.value.includes(code))
}

// 检查分组是否部分选中
function isGroupIndeterminate(group: PermissionGroupDto): boolean {
  if (!group.permissions || group.permissions.length === 0) return false
  const allCodes = getAllGroupPermissionCodes(group)
  const checkedCount = allCodes.filter(code => selectedPermissions.value.includes(code)).length
  return checkedCount > 0 && checkedCount < allCodes.length
}

// 获取分组所有权限代码（包括子权限）
function getAllGroupPermissionCodes(group: PermissionGroupDto): string[] {
  const codes: string[] = []
  group.permissions?.forEach(perm => {
    if (perm.code) codes.push(perm.code)
    perm.children?.forEach(child => {
      if (child.code) codes.push(child.code)
    })
  })
  return codes
}

// 获取分组已选中的权限
function getGroupSelectedPermissions(group: PermissionGroupDto): string[] {
  const allCodes = getAllGroupPermissionCodes(group)
  return allCodes.filter(code => selectedPermissions.value.includes(code))
}

// 处理分组全选/取消
function handleGroupCheck(group: PermissionGroupDto, checked: boolean) {
  const groupCodes = getAllGroupPermissionCodes(group)
  
  if (checked) {
    const newPermissions = new Set([...selectedPermissions.value, ...groupCodes])
    selectedPermissions.value = Array.from(newPermissions)
  } else {
    selectedPermissions.value = selectedPermissions.value.filter(p => !groupCodes.includes(p))
  }
}

// 检查单个权限是否选中
function isPermissionChecked(code: string | null | undefined): boolean {
  return code ? selectedPermissions.value.includes(code) : false
}

// 检查菜单权限是否部分选中（有子权限被选中但不是全部）
function isMenuIndeterminate(perm: PermissionDefinitionDto): boolean {
  if (!perm.children || perm.children.length === 0) return false
  const childCodes = perm.children.map(c => c.code).filter(Boolean) as string[]
  const checkedCount = childCodes.filter(code => selectedPermissions.value.includes(code)).length
  const menuChecked = perm.code ? selectedPermissions.value.includes(perm.code) : false
  // 如果菜单本身没选中，但有子权限选中，显示半选状态
  if (!menuChecked && checkedCount > 0) return true
  // 如果菜单选中了，但子权限没全选，也显示半选状态
  if (menuChecked && checkedCount > 0 && checkedCount < childCodes.length) return true
  return false
}

// 处理菜单权限变化（同时处理子权限）
function handleMenuPermissionChange(perm: PermissionDefinitionDto, checked: boolean) {
  const codes: string[] = []
  if (perm.code) codes.push(perm.code)
  perm.children?.forEach(child => {
    if (child.code) codes.push(child.code)
  })
  
  if (checked) {
    const newPermissions = new Set([...selectedPermissions.value, ...codes])
    selectedPermissions.value = Array.from(newPermissions)
  } else {
    selectedPermissions.value = selectedPermissions.value.filter(p => !codes.includes(p))
  }
}

// 处理按钮权限变化
function handleButtonPermissionChange(code: string | null | undefined, checked: boolean) {
  if (!code) return
  if (checked) {
    if (!selectedPermissions.value.includes(code)) {
      selectedPermissions.value = [...selectedPermissions.value, code]
    }
  } else {
    selectedPermissions.value = selectedPermissions.value.filter(p => p !== code)
  }
}

// 处理单个权限变化（保留兼容）
function handlePermissionChange(group: PermissionGroupDto, values: string[]) {
  const groupCodes = getAllGroupPermissionCodes(group)
  const otherPermissions = selectedPermissions.value.filter(p => !groupCodes.includes(p))
  selectedPermissions.value = [...otherPermissions, ...values]
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
      await RoleService.putRbacRoleUpdate({ id: currentId.value, requestBody: updateData })
      message.success('更新成功')
    } else {
      await RoleService.postRbacRoleCreate({ requestBody: formState })
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
    await RoleService.deleteRbacRoleDelete({ id })
    message.success('删除成功')
    loadData()
  } catch (error) {
    handleApiError(error)
  }
}

async function handleAssignPermissions(record: RoleDto) {
  currentRoleId.value = record.id!
  try {
    const permissions = await RoleService.getRbacRoleGetRolePermissions({ roleId: record.id })
    selectedPermissions.value = permissions
    permissionModalVisible.value = true
  } catch (error) {
    handleApiError(error)
  }
}

async function handlePermissionModalOk() {
  permissionModalLoading.value = true
  try {
    await RoleService.postRbacRoleAssignPermissions({
      roleId: currentRoleId.value,
      requestBody: selectedPermissions.value
    })
    message.success('分配权限成功')
    permissionModalVisible.value = false
    loadData()
  } catch (error) {
    handleApiError(error)
  } finally {
    permissionModalLoading.value = false
  }
}

// 全选权限
function checkAllPermissions() {
  const allCodes: string[] = []
  permissionTree.value.forEach(group => {
    if (group.permissions) {
      group.permissions.forEach(p => {
        if (p.code) allCodes.push(p.code)
        p.children?.forEach(child => {
          if (child.code) allCodes.push(child.code)
        })
      })
    }
  })
  selectedPermissions.value = allCodes
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

.permission-container {
  max-height: 500px;
  overflow: auto;
}

.permission-actions {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid #f0f0f0;
}

.permission-groups {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.permission-group {
  background: #fafafa;
  border-radius: 8px;
  padding: 12px 16px;
}

.group-header {
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid #e8e8e8;
}

.group-name {
  font-weight: 600;
  font-size: 14px;
}

.group-permissions {
  padding-left: 24px;
}

/* 菜单权限一行一行展示 */
.menu-permission-row {
  margin-bottom: 12px;
}

.menu-permission-row:last-child {
  margin-bottom: 0;
}

.menu-permission {
  margin-bottom: 4px;
}

.menu-permission :deep(.ant-checkbox-wrapper) {
  font-weight: 500;
  color: #333;
}

/* 按钮权限横向展示，有缩进 */
.button-permissions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 16px;
  padding-left: 24px;
  margin-top: 4px;
}

.button-permission-item {
  margin: 0 !important;
  color: #666;
  font-size: 13px;
}
</style>
