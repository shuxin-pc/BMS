<template>
  <div class="customer-archive">
    <!-- 无任何选项卡权限：整页空态，避免打开成空壳 -->
    <div v-if="visibleTabs.length === 0" class="card">
      <el-empty description="暂无可查看的档案内容，请联系管理员分配权限" />
    </div>

    <template v-else>
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
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, type Component } from 'vue'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { getCustomerOptions } from '@/api/customer-profile'
import type { CustomerOption } from '@/api/customer-profile/types'
import BeautyProfilePanel from './components/BeautyProfilePanel.vue'
import BodyDataPanel from './components/BodyDataPanel.vue'
import ReactionPanel from './components/ReactionPanel.vue'
import PhotoPanel from './components/PhotoPanel.vue'

const userStore = useUserStore()

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
  permission: string
  /** 面板组件 */
  component: Component
}

/**
 * 选项卡定义与权限映射
 * 系统权限只有目录/菜单/按钮三级，选项卡级控制依托 Type=2 按钮权限实现
 */
const tabDefinitions: TabDefinition[] = [
  { name: 'beauty', label: '美容档案', permission: 'store:customer:archive:beauty', component: BeautyProfilePanel },
  { name: 'bodyData', label: '体型数据', permission: 'store:customer:archive:body-data', component: BodyDataPanel },
  { name: 'reaction', label: '反应记录', permission: 'store:customer:archive:reaction', component: ReactionPanel },
  { name: 'photo', label: '照片管理', permission: 'store:customer:archive:photo', component: PhotoPanel }
]

/** 当前用户有权查看的选项卡 */
const visibleTabs = computed(() =>
  tabDefinitions.filter(tab => userStore.hasPermission(tab.permission))
)

const activeTab = ref('')

/** 默认激活第一个有权限的选项卡，与 visibleTabs 保持单一同步入口 */
watch(
  visibleTabs,
  tabs => {
    if (tabs.length === 0) {
      activeTab.value = ''
      return
    }
    if (!tabs.some(t => t.name === activeTab.value)) {
      activeTab.value = tabs[0].name
    }
  },
  { immediate: true }
)

const loadCustomers = async () => {
  customerLoading.value = true
  try {
    customerOptions.value = await getCustomerOptions()
  } catch (error) {
    ElMessage.error('加载客户列表失败')
  } finally {
    customerLoading.value = false
  }
}

onMounted(() => {
  // 无任何选项卡权限时页面只显示空态，无需拉客户列表
  if (visibleTabs.value.length > 0) {
    loadCustomers()
  }
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
