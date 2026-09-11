<template>
  <div class="customer-archive">
    <!-- 客户上下文选择区：4 个选项卡共享此选择结果 -->
    <div class="card mb-20">
      <div class="customer-selector">
        <span class="selector-label">客户</span>
        <el-select
          v-model="currentCustomerId"
          placeholder="请选择客户"
          filterable
          clearable
          :loading="customerLoading"
          style="width: 280px"
        >
          <el-option
            v-for="item in customerOptions"
            :key="item.id"
            :label="`${item.name}（${item.phone}）`"
            :value="item.id"
          />
        </el-select>
      </div>
    </div>

    <!-- 选项卡区 -->
    <div class="card">
      <el-tabs v-model="activeTab">
        <el-tab-pane
          v-for="tab in visibleTabs"
          :key="tab.name"
          :label="tab.label"
          :name="tab.name"
        >
          <el-empty v-if="!selectedCustomer" description="请先在上方选择客户" />
          <!-- 懒加载：仅渲染当前激活的面板，避免一次并发 4 个请求 -->
          <component
            v-else-if="activeTab === tab.name"
            :is="tab.component"
            :customer-id="selectedCustomer.id"
            :customer-name="selectedCustomer.name"
          />
        </el-tab-pane>
      </el-tabs>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, type Component } from 'vue'
import { ElMessage } from 'element-plus'
import { getCustomerOptions } from '@/api/customer-profile'
import type { CustomerOption } from '@/api/customer-profile/types'
import BeautyProfilePanel from './components/BeautyProfilePanel.vue'
import BodyDataPanel from './components/BodyDataPanel.vue'
import ReactionPanel from './components/ReactionPanel.vue'
import PhotoPanel from './components/PhotoPanel.vue'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()

const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

/** 当前选中的客户ID，作为 4 个面板的共享上下文 */
const currentCustomerId = ref<number | undefined>(undefined)
const customerOptions = ref<CustomerOption[]>([])
const customerLoading = ref(false)

/** 选中的客户对象；面板需要 name 做只读展示，故整体下传而非只传 id */
const selectedCustomer = computed(() =>
  customerOptions.value.find(c => c.id === currentCustomerId.value)
)

interface TabDefinition {
  name: string
  label: string
  /** 面板组件 */
  component: Component
}

/** 服务档案 4 个选项卡，直接全部展示 */
const tabDefinitions: TabDefinition[] = [
  { name: 'beauty', label: '美容档案', component: BeautyProfilePanel },
  { name: 'bodyData', label: '体型数据', component: BodyDataPanel },
  { name: 'reaction', label: '反应记录', component: ReactionPanel },
  { name: 'photo', label: '照片管理', component: PhotoPanel }
]

/** 按按钮权限过滤可见选项卡（tab.name 即权限码末段） */
const visibleTabs = computed(() =>
  tabDefinitions.filter(tab => hasPermission(`store:customer:archive:${tab.name}`))
)

/** 默认激活第一个有权限的选项卡 */
const activeTab = ref(visibleTabs.value[0]?.name)

const loadCustomers = async () => {
  customerLoading.value = true
  try {
    customerOptions.value = await getCustomerOptions()
  } catch {
    ElMessage.error('加载客户列表失败')
  } finally {
    customerLoading.value = false
  }
}

onMounted(() => {
  loadCustomers()
})
</script>

<style scoped>
.customer-archive {
  width: 100%;
}

.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

.customer-selector {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 24px;
}

.selector-label {
  color: var(--text-tertiary);
  font-size: 14px;
}

:deep(.el-tabs) {
  padding: 0 24px;
}

/* Tabs样式 */
:deep(.el-tabs__item) {
  color: var(--text-secondary);
}

:deep(.el-tabs__item.is-active) {
  color: var(--primary);
}

:deep(.el-tabs__active-bar) {
  background-color: var(--primary);
}

:deep(.el-tabs__nav-wrap::after) {
  border-color: var(--border-primary);
}
</style>
