<template>
  <div class="care-management">
    <el-tabs v-model="activeTab" class="care-tabs">
      <!-- ==================== 生日提醒 ==================== -->
      <el-tab-pane label="生日提醒" name="birthday">
        <!-- 搜索区域 -->
        <div class="card mb-20">
          <div class="search-form">
            <el-form :inline="true" :model="birthdaySearchForm" class="search-form-inline">
              <el-form-item label="客户名称/手机号">
                <el-input
                  v-model="birthdaySearchForm.keyword"
                  placeholder="姓名或手机号"
                  clearable
                  style="width: 180px"
                />
              </el-form-item>
              <el-form-item label="关怀状态">
                <el-select v-model="birthdaySearchForm.careStatus" placeholder="全部" clearable style="width: 130px">
                  <el-option label="待关怀" :value="1" />
                  <el-option label="已关怀" :value="2" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="handleBirthdaySearch">
                  <el-icon><Search /></el-icon>
                  搜索
                </el-button>
                <el-button @click="handleBirthdayReset">
                  <el-icon><Refresh /></el-icon>
                  重置
                </el-button>
              </el-form-item>
            </el-form>
          </div>
        </div>

        <!-- 操作栏 -->
        <div class="table-toolbar">
          <div class="toolbar-right">
            <el-button circle @click="loadBirthdayData">
              <el-icon><Refresh /></el-icon>
            </el-button>
          </div>
        </div>

        <!-- 表格区域 -->
        <div class="card">
          <el-table
            v-loading="birthdayLoading"
            :data="birthdayData"
            style="width: 100%"
          >
            <el-table-column prop="customerName" label="客户名称" min-width="120" />
            <el-table-column prop="phone" label="手机号" min-width="140" />
            <el-table-column prop="birthday" label="生日" width="100" align="center" />
            <el-table-column label="距离生日" width="110" align="center">
              <template #default="{ row }">
                <el-tag
                  :type="birthdayDaysTagType(row.daysToBirthday)"
                  size="small"
                  effect="dark"
                >
                  {{ birthdayDaysText(row.daysToBirthday) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="关怀状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.careStatus === 2 ? 'success' : 'warning'" size="small" effect="plain">
                  {{ row.careStatus === 2 ? '已关怀' : '待关怀' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="careTime" label="关怀时间" min-width="170">
              <template #default="{ row }">
                <span v-if="row.careTime">{{ formatDateTime(row.careTime) }}</span>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column label="操作人" width="100" align="center">
              <template #default="{ row }">
                <span v-if="row.careStatus === 2 && row.operatorName">{{ row.operatorName }}</span>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="120" fixed="right">
              <template #default="{ row }">
                <el-button
                  v-if="row.careStatus === 1 && hasPermission('store:customer:care:markCared')"
                  link
                  type="primary"
                  size="small"
                  @click="handleMarkCared(row)"
                >
                  <el-icon><Check /></el-icon>
                  标记关怀
                </el-button>
                <span v-else class="text-muted">已完成</span>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分页 -->
          <div class="pagination-container">
            <el-pagination
              v-model:current-page="birthdayPagination.pageIndex"
              v-model:page-size="birthdayPagination.pageSize"
              :page-sizes="systemConfigStore.defaultPageSizes"
              :total="birthdayPagination.total"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadBirthdayData"
              @current-change="loadBirthdayData"
            />
          </div>
        </div>
      </el-tab-pane>

      <!-- ==================== 消费感谢 ==================== -->
      <el-tab-pane label="消费感谢" name="thanks">
        <!-- 搜索区域 -->
        <div class="card mb-20">
          <div class="search-form">
            <el-form :inline="true" :model="thanksSearchForm" class="search-form-inline">
              <el-form-item label="客户名称/手机号">
                <el-input
                  v-model="thanksSearchForm.keyword"
                  placeholder="姓名或手机号"
                  clearable
                  style="width: 180px"
                />
              </el-form-item>
              <el-form-item label="感谢状态">
                <el-select v-model="thanksSearchForm.thankStatus" placeholder="全部" clearable style="width: 130px">
                  <el-option label="待感谢" :value="1" />
                  <el-option label="已感谢" :value="2" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="handleThanksSearch">
                  <el-icon><Search /></el-icon>
                  搜索
                </el-button>
                <el-button @click="handleThanksReset">
                  <el-icon><Refresh /></el-icon>
                  重置
                </el-button>
              </el-form-item>
            </el-form>
          </div>
        </div>

        <!-- 操作栏 -->
        <div class="table-toolbar">
          <div class="toolbar-right">
            <el-button circle @click="loadThanksData">
              <el-icon><Refresh /></el-icon>
            </el-button>
          </div>
        </div>

        <!-- 表格区域 -->
        <div class="card">
          <el-table
            v-loading="thanksLoading"
            :data="thanksData"
            style="width: 100%"
          >
            <el-table-column prop="customerName" label="客户名称" min-width="120" />
            <el-table-column prop="phone" label="手机号" min-width="140" />
            <el-table-column label="近7天累计" width="130" align="right">
              <template #default="{ row }">
                <span class="price-text">¥{{ formatPrice(row.totalAmount) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="消费频次" width="90" align="center">
              <template #default="{ row }">
                <span>{{ row.orderCount }} 次</span>
              </template>
            </el-table-column>
            <el-table-column prop="lastConsumeTime" label="最近消费时间" min-width="170">
              <template #default="{ row }">
                <span>{{ formatDateTime(row.lastConsumeTime) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="感谢状态" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="row.thankStatus === 2 ? 'success' : 'warning'" size="small" effect="plain">
                  {{ row.thankStatus === 2 ? '已感谢' : '待感谢' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="感谢方式" width="100" align="center">
              <template #default="{ row }">
                <span v-if="row.thankMethod">{{ thankMethodText(row.thankMethod) }}</span>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column prop="thankTime" label="感谢时间" min-width="170">
              <template #default="{ row }">
                <span v-if="row.thankTime">{{ formatDateTime(row.thankTime) }}</span>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column label="操作人" width="100" align="center">
              <template #default="{ row }">
                <span v-if="row.thankStatus === 2 && row.operatorName">{{ row.operatorName }}</span>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" fixed="right">
              <template #default="{ row }">
                <el-button
                  v-if="row.thankStatus === 1 && hasPermission('store:customer:care:markThanked')"
                  link
                  type="primary"
                  size="small"
                  @click="handleMarkThanked(row)"
                >
                  <el-icon><Check /></el-icon>
                  标记感谢
                </el-button>
                <span v-else class="text-muted">已完成</span>
              </template>
            </el-table-column>
          </el-table>

          <!-- 分页 -->
          <div class="pagination-container">
            <el-pagination
              v-model:current-page="thanksPagination.pageIndex"
              v-model:page-size="thanksPagination.pageSize"
              :page-sizes="systemConfigStore.defaultPageSizes"
              :total="thanksPagination.total"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadThanksData"
              @current-change="loadThanksData"
            />
          </div>
        </div>
      </el-tab-pane>
    </el-tabs>

    <!-- 感谢方式选择弹窗 -->
    <el-dialog
      v-model="thankDialogVisible"
      title="选择感谢方式"
      width="420px"
      :close-on-click-modal="false"
    >
      <el-form label-width="80px">
        <el-form-item label="客户">
          <span>{{ currentThankRecord?.customerName }}</span>
        </el-form-item>
        <el-form-item label="感谢方式">
          <el-radio-group v-model="selectedThankMethod">
            <el-radio :value="1">短信</el-radio>
            <el-radio :value="2">微信</el-radio>
            <el-radio :value="3">电话</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="thankDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="thankLoading" @click="handleThankSubmit">
          确认
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, Check } from '@element-plus/icons-vue'
import {
  getBirthdayReminders,
  markBirthdayCared,
  getConsumeThanks,
  markConsumeThanked
} from '@/api/customer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import { formatDateTime } from '@/utils/date'
import type { BirthdayReminder, ConsumeThankRecord } from '@/api/customer/types'

const systemConfigStore = useSystemConfigStore()

const userStore = useUserStore()

const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

const activeTab = ref('birthday')

// ==================== 生日提醒 ====================
const birthdayLoading = ref(false)
const birthdayData = ref<BirthdayReminder[]>([])
const birthdaySearchForm = reactive({
  keyword: '',
  careStatus: undefined as number | undefined
})
const birthdayPagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

const loadBirthdayData = async () => {
  birthdayLoading.value = true
  try {
    const res = await getBirthdayReminders({
      keyword: birthdaySearchForm.keyword || undefined,
      careStatus: birthdaySearchForm.careStatus,
      pageIndex: birthdayPagination.pageIndex,
      pageSize: birthdayPagination.pageSize
    })
    birthdayData.value = res.list
    birthdayPagination.total = res.total
  } catch (error) {
    ElMessage.error((error as Error).message || '加载失败')
  } finally {
    birthdayLoading.value = false
  }
}

const handleBirthdaySearch = () => {
  birthdayPagination.pageIndex = 1
  loadBirthdayData()
}

const handleBirthdayReset = () => {
  birthdaySearchForm.keyword = ''
  birthdaySearchForm.careStatus = undefined
  handleBirthdaySearch()
}

const handleMarkCared = async (row: BirthdayReminder) => {
  try {
    await markBirthdayCared(row.id)
    ElMessage.success('已标记为已关怀')
    loadBirthdayData()
  } catch (error) {
    ElMessage.error((error as Error).message || '操作失败')
  }
}

// ==================== 消费感谢 ====================
const thanksLoading = ref(false)
const thanksData = ref<ConsumeThankRecord[]>([])
const thanksSearchForm = reactive({
  keyword: '',
  thankStatus: undefined as number | undefined
})
const thanksPagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

const loadThanksData = async () => {
  thanksLoading.value = true
  try {
    const res = await getConsumeThanks({
      keyword: thanksSearchForm.keyword || undefined,
      thankStatus: thanksSearchForm.thankStatus,
      pageIndex: thanksPagination.pageIndex,
      pageSize: thanksPagination.pageSize
    })
    thanksData.value = res.list
    thanksPagination.total = res.total
  } catch (error) {
    ElMessage.error((error as Error).message || '加载失败')
  } finally {
    thanksLoading.value = false
  }
}

const handleThanksSearch = () => {
  thanksPagination.pageIndex = 1
  loadThanksData()
}

const handleThanksReset = () => {
  thanksSearchForm.keyword = ''
  thanksSearchForm.thankStatus = undefined
  handleThanksSearch()
}

// 感谢弹窗
const thankDialogVisible = ref(false)
const thankLoading = ref(false)
const currentThankRecord = ref<ConsumeThankRecord>()
const selectedThankMethod = ref(2)

const handleMarkThanked = (row: ConsumeThankRecord) => {
  currentThankRecord.value = row
  selectedThankMethod.value = 2
  thankDialogVisible.value = true
}

const handleThankSubmit = async () => {
  if (!currentThankRecord.value) return
  thankLoading.value = true
  try {
    await markConsumeThanked(currentThankRecord.value.id, selectedThankMethod.value)
    ElMessage.success('已标记为已感谢')
    thankDialogVisible.value = false
    loadThanksData()
  } catch (error) {
    ElMessage.error((error as Error).message || '操作失败')
  } finally {
    thankLoading.value = false
  }
}

// ==================== 工具方法 ====================

/** 格式化价格 */
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}

/** 感谢方式文本 */
const thankMethodText = (method: number) => {
  const map: Record<number, string> = { 1: '短信', 2: '微信', 3: '电话' }
  return map[method] || '未知'
}

/** 距离生日天数文本 */
const birthdayDaysText = (days: number) => {
  if (days === 0) return '今天'
  if (days > 0) return `${days}天后`
  return `已过${Math.abs(days)}天`
}

/** 距离生日标签类型 */
const birthdayDaysTagType = (days: number) => {
  if (days === 0) return 'danger'
  if (days > 0 && days <= 3) return 'warning'
  if (days > 3 && days <= 7) return ''
  return 'info'
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  birthdayPagination.pageSize = systemConfigStore.defaultPageSize
  thanksPagination.pageSize = systemConfigStore.defaultPageSize
  loadBirthdayData()
  loadThanksData()
})
</script>

<style scoped>
.care-management {
  width: 100%;
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

/* 卡片样式 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

/* 表格样式 */
:deep(.el-table) {
  --el-table-bg-color: transparent !important;
  --el-table-text-color: var(--text-primary) !important;
  --el-table-border-color: transparent !important;
  --el-table-header-bg-color: var(--bg-tertiary) !important;
  --el-table-row-hover-bg-color: var(--bg-hover) !important;
}

:deep(.el-table th.el-table__cell) {
  background: var(--bg-tertiary) !important;
  color: var(--text-tertiary) !important;
  font-weight: 600;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table td.el-table__cell) {
  background-color: var(--bg-tertiary) !important;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table__row:hover > td.el-table__cell) {
  background-color: var(--bg-hover) !important;
}

/* 搜索区域 */
.search-form {
  padding: 20px 24px 0;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

/* 操作栏 */
.table-toolbar {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  margin-bottom: 16px;
  padding: 0 4px;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 金额样式 */
.price-text {
  color: var(--primary);
  font-weight: 600;
}

.text-muted {
  color: var(--text-tertiary);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
