<template>
  <div class="menu-page">
    <div class="page-header">
      <a-space>
        <a-button
          v-if="hasPermission('Rbac.Menus.Create')"
          type="primary"
          @click="handleAdd()"
        >
          <template #icon><plus-outlined /></template>
          新增菜单
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
      :default-expand-all-rows="false"
      :expanded-row-keys="expandedKeys"
      @expand="handleExpand"
      row-key="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'type'">
          <a-tag :color="record.type === 1 ? 'blue' : 'green'">
            {{ record.type === 1 ? '菜单' : '按钮' }}
          </a-tag>
        </template>

        <template v-else-if="column.key === 'icon'">
          <component v-if="record.icon" :is="getIcon(record.icon)" />
        </template>

        <template v-else-if="column.key === 'isHidden'">
          <a-tag :color="record.isHidden ? 'red' : 'green'">
            {{ record.isHidden ? '隐藏' : '显示' }}
          </a-tag>
        </template>

        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button
              v-if="hasPermission('Rbac.Menus.Create')"
              type="link"
              size="small"
              @click="handleAdd(record.id)"
            >
              新增子菜单
            </a-button>
            <a-button
              v-if="hasPermission('Rbac.Menus.Update')"
              type="link"
              size="small"
              @click="handleEdit(record)"
            >
              编辑
            </a-button>
            <a-popconfirm
              v-if="hasPermission('Rbac.Menus.Delete')"
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

    <!-- 新增/编辑菜单弹窗 -->
    <a-modal
      v-model:open="modalVisible"
      :title="modalTitle"
      :confirm-loading="modalLoading"
      @ok="handleModalOk"
      width="700px"
    >
      <a-form
        ref="formRef"
        :model="formState"
        :rules="formRules"
        :label-col="{ span: 6 }"
        :wrapper-col="{ span: 16 }"
      >
        <a-form-item label="父级菜单" name="pId">
          <a-tree-select
            v-model:value="formState.pId"
            :tree-data="menuTreeData"
            placeholder="请选择父级菜单（不选则为顶级菜单）"
            allow-clear
            :field-names="{ label: 'name', value: 'id', children: 'children' }"
          />
        </a-form-item>

        <a-form-item label="菜单类型" name="type">
          <a-radio-group v-model:value="formState.type">
            <a-radio :value="1">菜单</a-radio>
            <a-radio :value="2">按钮</a-radio>
          </a-radio-group>
        </a-form-item>

        <a-form-item label="菜单名称" name="name">
          <a-input v-model:value="formState.name" placeholder="请输入菜单名称" />
        </a-form-item>

        <a-form-item label="权限编码" name="permissionCode">
          <a-input v-model:value="formState.permissionCode" placeholder="请输入权限编码" />
        </a-form-item>

        <a-form-item v-if="formState.type === 1" label="图标" name="icon">
          <a-input v-model:value="formState.icon" placeholder="请输入图标名称" />
        </a-form-item>

        <a-form-item v-if="formState.type === 1" label="路由" name="route">
          <a-input v-model:value="formState.route" placeholder="请输入路由路径" />
        </a-form-item>

        <a-form-item v-if="formState.type === 1" label="组件" name="component">
          <a-input v-model:value="formState.component" placeholder="请输入组件路径" />
        </a-form-item>

        <a-form-item label="排序" name="order">
          <a-input-number
            v-model:value="formState.order"
            :min="0"
            style="width: 100%"
          />
        </a-form-item>

        <a-form-item v-if="formState.type === 1" label="是否隐藏" name="isHidden">
          <a-switch v-model:checked="formState.isHidden" />
        </a-form-item>

        <a-form-item v-if="formState.type === 1" label="是否缓存" name="isCached">
          <a-switch v-model:checked="formState.isCached" />
        </a-form-item>

        <a-form-item label="描述" name="description">
          <a-textarea
            v-model:value="formState.description"
            placeholder="请输入描述"
            :rows="3"
          />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { MenuService } from '@/api'
import type { MenuDto, MenuTreeDto, MenuCreateDto, MenuUpdateDto, MenuType } from '@/api'
import { hasPermission } from '@/utils/permission'
import { handleApiError } from '@/utils/error-handler'
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons-vue'
import * as Icons from '@ant-design/icons-vue'
import type { TableColumnsType } from 'ant-design-vue'

const loading = ref(false)
const dataSource = ref<MenuTreeDto[]>([])
const expandedKeys = ref<string[]>([])

const columns: TableColumnsType = [
  { title: '菜单名称', dataIndex: 'name', key: 'name', width: 200 },
  { title: '类型', key: 'type', width: 80 },
  { title: '图标', key: 'icon', width: 80 },
  { title: '权限编码', dataIndex: 'permissionCode', key: 'permissionCode' },
  { title: '路由', dataIndex: 'route', key: 'route' },
  { title: '排序', dataIndex: 'order', key: 'order', width: 80 },
  { title: '状态', key: 'isHidden', width: 80 },
  { title: '操作', key: 'action', width: 280 }
]

// 表单相关
const modalVisible = ref(false)
const modalLoading = ref(false)
const modalTitle = ref('新增菜单')
const isEdit = ref(false)
const formRef = ref()
const currentId = ref('')

const formState = reactive<MenuCreateDto>({
  pId: undefined,
  name: '',
  type: 1, // MenuType = 1 | 2
  permissionCode: '',
  icon: '',
  route: '',
  component: '',
  order: 0,
  isHidden: false,
  isCached: false,
  description: ''
})

const formRules = {
  name: [{ required: true, message: '请输入菜单名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择菜单类型', trigger: 'change' }]
}

const menuTreeData = computed(() => {
  return dataSource.value
})

onMounted(() => {
  loadData()
})

async function loadData() {
  loading.value = true
  try {
    dataSource.value = await MenuService.getRbacMenuGetMenuTree()
    // 默认展开前两级
    expandedKeys.value = getDefaultExpandedKeys(dataSource.value, 2)
  } catch (error) {
    handleApiError(error)
  } finally {
    loading.value = false
  }
}

// 获取默认展开的节点（展开前 N 级）
function getDefaultExpandedKeys(data: MenuTreeDto[], maxLevel: number, currentLevel = 1): string[] {
  const keys: string[] = []
  if (currentLevel > maxLevel) return keys
  
  data.forEach(item => {
    if (item.id && item.children && item.children.length > 0) {
      keys.push(item.id)
      // 递归获取子节点
      keys.push(...getDefaultExpandedKeys(item.children, maxLevel, currentLevel + 1))
    }
  })
  return keys
}

// 处理展开/收起
function handleExpand(expanded: boolean, record: MenuTreeDto) {
  if (expanded) {
    if (record.id && !expandedKeys.value.includes(record.id)) {
      expandedKeys.value.push(record.id)
    }
  } else {
    // 收起时，同时收起所有子节点
    if (record.id) {
      const keysToRemove = new Set<string>([record.id])
      const collectChildKeys = (item: MenuTreeDto) => {
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

function handleAdd(parentId?: string) {
  modalTitle.value = '新增菜单'
  isEdit.value = false
  Object.assign(formState, {
    pId: parentId,
    name: '',
    type: 1 as MenuType,
    permissionCode: '',
    icon: '',
    route: '',
    component: '',
    order: 0,
    isHidden: false,
    isCached: false,
    description: ''
  })
  modalVisible.value = true
}

function handleEdit(record: MenuDto) {
  modalTitle.value = '编辑菜单'
  isEdit.value = true
  currentId.value = record.id || ''
  Object.assign(formState, {
    pId: record.pId,
    name: record.name,
    type: record.type,
    permissionCode: record.permissionCode,
    icon: record.icon,
    route: record.route,
    component: record.component,
    order: record.order,
    isHidden: record.isHidden,
    isCached: record.isCached,
    description: record.description
  })
  modalVisible.value = true
}

async function handleModalOk() {
  try {
    await formRef.value.validate()
    modalLoading.value = true

    if (isEdit.value) {
      const updateData: MenuUpdateDto = {
        name: formState.name,
        permissionCode: formState.permissionCode,
        icon: formState.icon,
        route: formState.route,
        component: formState.component,
        isHidden: formState.isHidden,
        isCached: formState.isCached,
        description: formState.description
      }
      await MenuService.putRbacMenuUpdate({ id: currentId.value, requestBody: updateData })
      message.success('更新成功')
    } else {
      await MenuService.postRbacMenuCreate({ requestBody: formState })
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
    await MenuService.deleteRbacMenuDelete({ id })
    message.success('删除成功')
    loadData()
  } catch (error) {
    handleApiError(error)
  }
}

function getIcon(iconName: string) {
  return (Icons as any)[iconName] || null
}

// 展开全部
function expandAll() {
  const getAllKeys = (data: MenuTreeDto[]): string[] => {
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

// 收起全部
function collapseAll() {
  expandedKeys.value = []
}
</script>

<style scoped>
.menu-page {
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
