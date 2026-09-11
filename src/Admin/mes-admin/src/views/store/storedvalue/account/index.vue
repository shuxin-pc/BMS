<template>
  <div class="account-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称/手机号">
            <el-input
              v-model="searchForm.keyword"
              placeholder="姓名或手机号"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearch">
              <el-icon><Search /></el-icon>
              搜索
            </el-button>
            <el-button @click="handleReset">
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
        <el-button circle @click="loadData">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <!-- 表格区域 -->
    <div class="card">
      <el-table
        v-loading="tableLoading"
        :data="tableData"
        style="width: 100%"
      >
        <el-table-column prop="customerName" label="客户名称" min-width="120" />
        <el-table-column prop="phone" label="手机号" min-width="140" />
        <el-table-column label="当前余额" width="120" align="right">
          <template #default="{ row }">
            <span class="balance-text">¥{{ formatPrice(row.balance) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="实收余额" width="120" align="right">
          <template #default="{ row }">
            <span>¥{{ formatPrice(row.realBalance) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="赠送余额" width="120" align="right">
          <template #default="{ row }">
            <span class="bonus-text">¥{{ formatPrice(row.giftBalance) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="累计充值" width="120" align="right">
          <template #default="{ row }">
            <span>¥{{ formatPrice(row.totalRecharge) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="累计消费" width="120" align="right">
          <template #default="{ row }">
            <span class="consume-text">¥{{ formatPrice(row.totalConsume) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="累计赠送" width="120" align="right">
          <template #default="{ row }">
            <span class="bonus-text">¥{{ formatPrice(row.totalGift) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="开户时间" min-width="170">
          <template #default="{ row }">
            <span>{{ formatDateTime(row.createdAt) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleRecharge(row)" v-if="hasPermission('store:storedvalue:account:recharge')">
              <el-icon><Wallet /></el-icon>
              充值
            </el-button>
            <el-button link type="danger" size="small" @click="handleRefund(row)" v-if="hasPermission('store:storedvalue:account:refund')">
              <el-icon><RefreshLeft /></el-icon>
              退款
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="pagination.pageIndex"
          v-model:page-size="pagination.pageSize"
          :page-sizes="systemConfigStore.defaultPageSizes"
          :total="pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>

    <!-- 充值弹窗（与客户详情共用同一组件） -->
    <RechargeDialog
      v-model="rechargeVisible"
      :customer-id="rechargeTarget.customerId"
      :customer-name="rechargeTarget.customerName"
      :current-balance="rechargeTarget.balance"
      @success="loadData"
    />

    <!-- 退款弹窗 -->
    <RefundDialog
      v-model="refundVisible"
      :customer-id="refundTarget.customerId"
      :customer-name="refundTarget.customerName"
      :real-balance="refundTarget.realBalance"
      :gift-balance="refundTarget.giftBalance"
      @success="loadData"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Search, Refresh, Wallet, RefreshLeft } from '@element-plus/icons-vue'
import { getMemberAccounts } from '@/api/member'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { MemberAccount } from '@/api/member/types'
import RechargeDialog from '../components/RechargeDialog.vue'
import RefundDialog from '../components/RefundDialog.vue'
import { formatDateTime } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  keyword: ''
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<MemberAccount[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getMemberAccounts({
      keyword: searchForm.keyword || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error((error as Error).message || '加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.keyword = ''
  handleSearch()
}

// ==================== 充值 ====================
const rechargeVisible = ref(false)
const rechargeTarget = reactive({
  customerId: 0,
  customerName: '',
  balance: 0
})

const handleRecharge = (row: MemberAccount) => {
  rechargeTarget.customerId = row.customerId
  rechargeTarget.customerName = row.customerName || ''
  rechargeTarget.balance = row.balance
  rechargeVisible.value = true
}

// ==================== 退款 ====================
const refundVisible = ref(false)
const refundTarget = reactive({
  customerId: 0,
  customerName: '',
  realBalance: 0,
  giftBalance: 0
})

const handleRefund = (row: MemberAccount) => {
  // 文档 G7：只退实收、赠送一律不退；实收余额为 0 时无可退金额。
  // 提前拦截避免打开弹窗时 InputNumber min(0.01) > max(0) 触发 ElementPlusError
  if (row.realBalance <= 0) {
    ElMessage.warning('实收余额为 0，无可退金额（赠送余额一律不退）')
    return
  }
  refundTarget.customerId = row.customerId
  refundTarget.customerName = row.customerName || ''
  refundTarget.realBalance = row.realBalance
  refundTarget.giftBalance = row.giftBalance
  refundVisible.value = true
}

// ==================== 工具方法 ====================

/** 格式化价格 */
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}


// 全局搜索跳转预填：读取路由 keyword 参数回填搜索框
const route = useRoute()
const routeKeyword = typeof route.query.keyword === 'string' ? route.query.keyword : ''

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  if (routeKeyword) {
    searchForm.keyword = routeKeyword
  }
  loadData()
})
</script>

<style scoped>
.account-management {
  width: 100%;
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
.balance-text {
  color: var(--primary);
  font-weight: 600;
}

.consume-text {
  color: var(--el-color-danger);
}

.bonus-text {
  color: var(--el-color-success);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
