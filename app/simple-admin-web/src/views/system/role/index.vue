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

        <a-form-item label="描述" name="description">
          <a-textarea
            v-model:value="formState.description"
            placeholder="请输入描述"
            :rows="4"
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
      <a-tree
        v-model:checkedKeys="selectedPermissions"
        checkable
        :tree-data="permissionTreeData"
        :field-names="{ title: 'name', key: 'code', children: 'permissions' }"
      />
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { RoleService, PermissionService } from '@/api'
import type { RoleDto, RoleCreateDto, RoleUpdateDto, PermissionGroupDto } from '@/api'
import { hasPermission } from '@/utils/permission'
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
  { title: '描述', dataIndex: 'description', key: 'description' },
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
  code: '',
  description: ''
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

const permissionTreeData = computed(() => permissionTree.value)

onMounted(() => {
  loadData()
  loadPermissions()
})

async function loadData() {
  loading.value = true
  try {
    const result = await RoleService.getRbacRoleGetList({
      page: pagination.current,
      pageSize: pagination.pageSize
    })
    dataSource.value = result.items || []
    pagination.total = result.total || 0
  } catch (error) {
    message.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

async function loadPermissions() {
  try {
    permissionTree.value = await PermissionService.getRbacPermissionGetPermissionTree()
  } catch (error) {
    message.error('加载权限失败')
  }
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
    code: '',
    description: ''
  })
  modalVisible.value = true
}

function handleEdit(record: RoleDto) {
  modalTitle.value = '编辑角色'
  isEdit.value = true
  currentId.value = record.id!
  Object.assign(formState, {
    name: record.name,
    code: record.code,
    description: record.description
  })
  modalVisible.value = true
}

async function handleModalOk() {
  try {
    await formRef.value.validate()
    modalLoading.value = true

    if (isEdit.value) {
      const updateData: RoleUpdateDto = {
        name: formState.name,
        code: formState.code,
        description: formState.description
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
    message.error(error.message || '操作失败')
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
    message.error('删除失败')
  }
}

async function handleAssignPermissions(record: RoleDto) {
  currentRoleId.value = record.id!
  try {
    const permissions = await RoleService.getRbacRoleGetRolePermissions({ roleId: record.id })
    selectedPermissions.value = permissions
    permissionModalVisible.value = true
  } catch (error) {
    message.error('加载角色权限失败')
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
    message.error('分配权限失败')
  } finally {
    permissionModalLoading.value = false
  }
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
</style>
