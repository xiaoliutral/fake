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
          <component v-if="record.icon && getIconComponent(record.icon)" :is="getIconComponent(record.icon)" />
          <span v-else-if="record.icon" class="icon-text">{{ record.icon }}</span>
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
          <a-select
            v-model:value="formState.icon"
            placeholder="请选择图标"
            allow-clear
            show-search
            :filter-option="filterIconOption"
          >
            <a-select-option v-for="icon in iconList" :key="icon.name" :value="icon.name">
              <span class="icon-option">
                <component :is="icon.component" />
                <span class="icon-name">{{ icon.name }}</span>
              </span>
            </a-select-option>
          </a-select>
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
import { ref, reactive, onMounted, computed, type Component as VueComponent } from 'vue'
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

// 图标名称映射
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
  'key': 'KeyOutlined',
  'lock': 'LockOutlined',
  'safety': 'SafetyOutlined',
  'tool': 'ToolOutlined'
}

// 获取图标组件
function getIconComponent(iconName: string | null | undefined): VueComponent | null {
  if (!iconName) return null
  
  let mappedName = iconNameMap[iconName.toLowerCase()]
  
  if (!mappedName) {
    if (iconName.includes('Outlined') || iconName.includes('Filled') || iconName.includes('TwoTone')) {
      mappedName = iconName
    } else {
      mappedName = iconName.charAt(0).toUpperCase() + iconName.slice(1) + 'Outlined'
    }
  }
  
  const icon = (Icons as Record<string, VueComponent>)[mappedName]
  return icon || null
}

// 常用图标列表
const iconList = computed(() => {
  const commonIcons = [
    'SettingOutlined', 'UserOutlined', 'TeamOutlined', 'MenuOutlined', 'ApartmentOutlined',
    'DashboardOutlined', 'HomeOutlined', 'FileOutlined', 'FolderOutlined', 'DatabaseOutlined',
    'CloudOutlined', 'ApiOutlined', 'BellOutlined', 'CalendarOutlined', 'ProfileOutlined',
    'TableOutlined', 'FormOutlined', 'KeyOutlined', 'LockOutlined', 'SafetyOutlined',
    'ToolOutlined', 'ShopOutlined', 'ShoppingCartOutlined', 'GiftOutlined', 'WalletOutlined',
    'BankOutlined', 'AuditOutlined', 'SolutionOutlined', 'IdcardOutlined', 'ContactsOutlined',
    'ScheduleOutlined', 'FileTextOutlined', 'ClusterOutlined', 'GlobalOutlined', 'EnvironmentOutlined',
    'RocketOutlined', 'ExperimentOutlined', 'BugOutlined', 'CodeOutlined', 'BuildOutlined',
    'BlockOutlined', 'LayoutOutlined', 'AppstoreOutlined', 'PieChartOutlined', 'BarChartOutlined',
    'LineChartOutlined', 'FundOutlined', 'MoneyCollectOutlined', 'InsuranceOutlined',
    'EditOutlined', 'DeleteOutlined', 'SaveOutlined', 'CopyOutlined', 'UploadOutlined',
    'DownloadOutlined', 'EyeOutlined', 'SearchOutlined', 'PlusOutlined', 'MinusOutlined'
  ]
  
  return commonIcons.map(name => ({
    name,
    component: (Icons as Record<string, VueComponent>)[name]
  })).filter(item => item.component)
})

// 图标搜索过滤
function filterIconOption(input: string, option: any) {
  return option.value.toLowerCase().includes(input.toLowerCase())
}

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
  type: 1,
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
  type: [{ required: true, message: '请选择菜单类型', trigger: 'change' }],
  permissionCode: [{ required: true, message: '请输入权限编码', trigger: 'blur' }]
}

const menuTreeData = computed(() => dataSource.value)

onMounted(() => {
  loadData()
})

async function loadData() {
  loading.value = true
  try {
    dataSource.value = await MenuService.getRbacMenuGetMenuTree()
    expandedKeys.value = getDefaultExpandedKeys(dataSource.value, 2)
  } catch (error) {
    handleApiError(error)
  } finally {
    loading.value = false
  }
}

function getDefaultExpandedKeys(data: MenuTreeDto[], maxLevel: number, currentLevel = 1): string[] {
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

function handleExpand(expanded: boolean, record: MenuTreeDto) {
  if (expanded) {
    if (record.id && !expandedKeys.value.includes(record.id)) {
      expandedKeys.value.push(record.id)
    }
  } else {
    if (record.id) {
      const keysToRemove = new Set<string>([record.id])
      const collectChildKeys = (item: MenuTreeDto) => {
        item.children?.forEach(child => {
          if (child.id) {
            keysToRemove.add(child.id)
            collectChildKeys(child)
          }
        })
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
    if (error.errorFields) return
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

function expandAll() {
  const getAllKeys = (data: MenuTreeDto[]): string[] => {
    const keys: string[] = []
    data.forEach(item => {
      if (item.id) keys.push(item.id)
      if (item.children?.length) keys.push(...getAllKeys(item.children))
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
.menu-page {
  background: #fff;
  border-radius: 8px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}

.icon-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.icon-name {
  font-size: 12px;
  color: #666;
}

.icon-text {
  font-size: 12px;
  color: #999;
}
</style>