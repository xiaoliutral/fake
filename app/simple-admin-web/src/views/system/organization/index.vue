<template>
  <div class="organization-page">
    <div class="page-header">
      <a-space>
        <a-button
          v-if="hasPermission('Rbac.Organizations.Create')"
          type="primary"
          @click="handleAdd()"
        >
          <template #icon><plus-outlined /></template>
          新增组织
        </a-button>
        <a-button @click="expandAll">
          展开全部
        </a-button>
        <a-button @click="collapseAll">
          收起全部
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
      :pagination="false"
      :expanded-row-keys="expandedKeys"
      @expand="handleExpand"
      row-key="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'type'">
          <a-tag :color="getTypeColor(record.type)">
            {{ getTypeName(record.type) }}
          </a-tag>
        </template>

        <template v-else-if="column.key === 'isEnabled'">
          <a-tag :color="record.isEnabled ? 'green' : 'red'">
            {{ record.isEnabled ? '启用' : '禁用' }}
          </a-tag>
        </template>

        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button
              v-if="hasPermission('Rbac.Organizations.Create')"
              type="link"
              size="small"
              @click="handleAdd(record.id)"
            >
              新增子组织
            </a-button>
            <a-button
              v-if="hasPermission('Rbac.Organizations.Update')"
              type="link"
              size="small"
              @click="handleEdit(record)"
            >
              编辑
            </a-button>
            <a-popconfirm
              v-if="hasPermission('Rbac.Organizations.Delete')"
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

    <!-- 新增/编辑组织弹窗 -->
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
        <a-form-item label="上级组织" name="parentId">
          <a-tree-select
            v-model:value="formState.parentId"
            :tree-data="treeSelectData"
            placeholder="请选择上级组织（不选则为顶级）"
            allow-clear
            :field-names="{ label: 'name', value: 'id', children: 'children' }"
          />
        </a-form-item>

        <a-form-item label="组织名称" name="name">
          <a-input v-model:value="formState.name" placeholder="请输入组织名称" />
        </a-form-item>

        <a-form-item label="组织编码" name="code">
          <a-input v-model:value="formState.code" placeholder="请输入组织编码" />
        </a-form-item>

        <a-form-item label="组织类型" name="type">
          <a-select v-model:value="formState.type" placeholder="请选择组织类型">
            <a-select-option :value="1">公司</a-select-option>
            <a-select-option :value="2">部门</a-select-option>
            <a-select-option :value="3">小组</a-select-option>
            <a-select-option :value="4">岗位</a-select-option>
          </a-select>
        </a-form-item>

        <a-form-item label="负责人" name="leaderId">
          <a-input v-model:value="formState.leaderId" placeholder="请输入负责人ID（可选）" />
        </a-form-item>

        <a-form-item label="排序" name="order">
          <a-input-number v-model:value="formState.order" :min="0" style="width: 100%" />
        </a-form-item>

        <a-form-item label="描述" name="description">
          <a-textarea v-model:value="formState.description" placeholder="请输入描述" :rows="3" />
        </a-form-item>

        <a-form-item label="是否启用" name="isEnabled">
          <a-switch v-model:checked="formState.isEnabled" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { OrganizationService } from '@/api'
import type { OrganizationDto, OrganizationTreeDto, OrganizationCreateDto, OrganizationUpdateDto } from '@/api'
import { hasPermission } from '@/utils/permission'
import { handleApiError } from '@/utils/error-handler'
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons-vue'
import type { TableColumnsType } from 'ant-design-vue'

const loading = ref(false)
const dataSource = ref<OrganizationTreeDto[]>([])
const expandedKeys = ref<string[]>([])

const columns: TableColumnsType = [
  { title: '组织名称', dataIndex: 'name', key: 'name', width: 200 },
  { title: '组织编码', dataIndex: 'code', key: 'code', width: 150 },
  { title: '类型', key: 'type', width: 100 },
  { title: '排序', dataIndex: 'order', key: 'order', width: 80 },
  { title: '状态', key: 'isEnabled', width: 80 },
  { title: '操作', key: 'action', width: 250 }
]

// 表单相关
const modalVisible = ref(false)
const modalLoading = ref(false)
const modalTitle = ref('新增组织')
const isEdit = ref(false)
const formRef = ref()
const currentId = ref('')

const formState = reactive<OrganizationCreateDto>({
  parentId: undefined,
  name: '',
  code: '',
  type: 2,
  leaderId: undefined,
  order: 0,
  description: '',
  isEnabled: true
})

const formRules = {
  name: [{ required: true, message: '请输入组织名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入组织编码', trigger: 'blur' }],
  type: [{ required: true, message: '请选择组织类型', trigger: 'change' }]
}

const treeSelectData = computed(() => dataSource.value)

onMounted(() => {
  loadData()
})

async function loadData() {
  loading.value = true
  try {
    dataSource.value = await OrganizationService.getRbacOrganizationGetTree()
    expandedKeys.value = getDefaultExpandedKeys(dataSource.value, 2)
  } catch (error) {
    handleApiError(error)
  } finally {
    loading.value = false
  }
}

function getDefaultExpandedKeys(data: OrganizationTreeDto[], maxLevel: number, currentLevel = 1): string[] {
  const keys: string[] = []
  if (currentLevel > maxLevel) return keys
  
  data.forEach(item => {
    if (item.id && item.children && item.children.length > 0) {
      keys.push(item.id)
      keys.push(...getDefaultExpandedKeys(item.children, maxLevel, currentLevel + 1))
    }
  })
  return keys
}

function handleExpand(expanded: boolean, record: OrganizationTreeDto) {
  if (expanded) {
    if (record.id && !expandedKeys.value.includes(record.id)) {
      expandedKeys.value.push(record.id)
    }
  } else {
    if (record.id) {
      const keysToRemove = new Set<string>([record.id])
      const collectChildKeys = (item: OrganizationTreeDto) => {
        if (item.children) {
          item.children.forEach(child => {
            if (child.id) {
              keysToRemove.add(child.id)
              collectChildKeys(child)
            }
          })
        }
      }
      collectChildKeys(record)
      expandedKeys.value = expandedKeys.value.filter(key => !keysToRemove.has(key))
    }
  }
}

function getTypeColor(type: number) {
  const colors: Record<number, string> = {
    1: 'blue',
    2: 'green',
    3: 'orange',
    4: 'purple'
  }
  return colors[type] || 'default'
}

function getTypeName(type: number) {
  const names: Record<number, string> = {
    1: '公司',
    2: '部门',
    3: '小组',
    4: '岗位'
  }
  return names[type] || '未知'
}

function handleAdd(parentId?: string) {
  modalTitle.value = '新增组织'
  isEdit.value = false
  Object.assign(formState, {
    parentId: parentId || undefined,
    name: '',
    code: '',
    type: 2,
    leaderId: undefined,
    order: 0,
    description: '',
    isEnabled: true
  })
  modalVisible.value = true
}

function handleEdit(record: OrganizationDto) {
  modalTitle.value = '编辑组织'
  isEdit.value = true
  currentId.value = record.id || ''
  Object.assign(formState, {
    parentId: record.parentId,
    name: record.name,
    code: record.code,
    type: record.type,
    leaderId: record.leaderId,
    order: record.order,
    description: record.description,
    isEnabled: record.isEnabled
  })
  modalVisible.value = true
}

async function handleModalOk() {
  try {
    await formRef.value.validate()
    modalLoading.value = true

    if (isEdit.value) {
      const updateData: OrganizationUpdateDto = {
        name: formState.name,
        type: formState.type,
        leaderId: formState.leaderId,
        order: formState.order,
        description: formState.description,
        isEnabled: formState.isEnabled
      }
      await OrganizationService.putRbacOrganizationUpdate({ id: currentId.value, requestBody: updateData })
      message.success('更新成功')
    } else {
      await OrganizationService.postRbacOrganizationCreate({ requestBody: formState })
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

async function handleDelete(id: string | undefined) {
  if (!id) return
  try {
    await OrganizationService.deleteRbacOrganizationDelete({ id })
    message.success('删除成功')
    loadData()
  } catch (error) {
    handleApiError(error)
  }
}

function expandAll() {
  const getAllKeys = (data: OrganizationTreeDto[]): string[] => {
    const keys: string[] = []
    data.forEach(item => {
      if (item.id) {
        keys.push(item.id)
      }
      if (item.children && item.children.length > 0) {
        keys.push(...getAllKeys(item.children))
      }
    })
    return keys
  }
  expandedKeys.value = getAllKeys(dataSource.value)
}

function collapseAll() {
  expandedKeys.value = []
}
</script>

<style scoped>
.organization-page {
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
