<template>
  <div class="cash-flow-page">
    <!-- 筛选区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="日期范围">
            <el-date-picker
              v-model="searchForm.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 260px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
            <el-button :icon="Refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stat-cards" v-loading="loading">
      <div class="stat-card recharge">
        <div class="stat-label">新增储值金额</div>
        <div class="stat-value">{{ formatMoney(cashFlow.totalRecharge) }}</div>
        <div class="stat-extra">赠送金额 {{ formatMoney(cashFlow.totalGift) }}</div>
      </div>
      <div class="stat-card consume">
        <div class="stat-label">储值消费金额</div>
        <div class="stat-value">{{ formatMoney(cashFlow.totalConsume) }}</div>
      </div>
      <div class="stat-card refund">
        <div class="stat-label">储值退款金额</div>
        <div class="stat-value">{{ formatMoney(cashFlow.totalRefund) }}</div>
      </div>
      <div class="stat-card balance">
        <div class="stat-label">沉淀资金（期末余额）</div>
        <div class="stat-value">{{ formatMoney(cashFlow.totalBalance) }}</div>
        <div class="stat-extra">
          实收 {{ formatMoney(cashFlow.totalRealBalance) }} / 赠送 {{ formatMoney(cashFlow.totalGiftBalance) }}
        </div>
      </div>
    </div>

    <!-- 净现金流 -->
    <div class="card mt-20">
      <div class="net-cash-flow">
        <span class="label">净储值现金流</span>
        <span class="value" :class="{ positive: netCashFlow >= 0, negative: netCashFlow < 0 }">
          {{ formatMoney(netCashFlow) }}
        </span>
        <span class="hint">= 新增储值 - 储值消费 - 储值退款</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import { getStoredValueCashFlow } from '@/api/member'
import type { StoredValueCashFlow } from '@/api/member/types'

const loading = ref(false)

const searchForm = reactive({
  dateRange: [] as string[]
})

const cashFlow = ref<StoredValueCashFlow>({
  totalRecharge: 0,
  totalGift: 0,
  totalConsume: 0,
  totalRefund: 0,
  totalBalance: 0,
  totalRealBalance: 0,
  totalGiftBalance: 0
})

/** 净储值现金流 = 新增储值 - 储值消费 - 储值退款 */
const netCashFlow = computed(() =>
  cashFlow.value.totalRecharge - cashFlow.value.totalConsume - cashFlow.value.totalRefund
)

/** 格式化金额 */
const formatMoney = (val: number) => {
  return val.toFixed(2)
}

/** 加载数据 */
const loadData = async () => {
  loading.value = true
  try {
    const startDate = searchForm.dateRange?.[0] || undefined
    const endDate = searchForm.dateRange?.[1] || undefined
    cashFlow.value = await getStoredValueCashFlow(startDate, endDate)
  } catch (error: any) {
    ElMessage.error(error.message || '加载统计数据失败')
  } finally {
    loading.value = false
  }
}

/** 查询 */
const handleSearch = () => {
  loadData()
}

/** 重置 */
const handleReset = () => {
  searchForm.dateRange = []
  loadData()
}

onMounted(() => {
  // 默认查询本月
  const now = new Date()
  const firstDay = new Date(now.getFullYear(), now.getMonth(), 1)
  searchForm.dateRange = [
    firstDay.toISOString().slice(0, 10),
    now.toISOString().slice(0, 10)
  ]
  loadData()
})
</script>

<style scoped>
.cash-flow-page {
  width: 100%;
}

.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
  padding: 20px;
}

.mb-20 {
  margin-bottom: 20px;
}

.mt-20 {
  margin-top: 20px;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

/* 统计卡片 */
.stat-cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
}

.stat-card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  padding: 24px;
  position: relative;
  overflow: hidden;
}

.stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 4px;
  height: 100%;
}

.stat-card.recharge::before { background: #67c23a; }
.stat-card.consume::before { background: #e6a23c; }
.stat-card.refund::before { background: #f56c6c; }
.stat-card.balance::before { background: #409eff; }

.stat-label {
  font-size: 14px;
  color: var(--text-tertiary);
  margin-bottom: 12px;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 8px;
}

.stat-extra {
  font-size: 12px;
  color: var(--text-tertiary);
}

/* 净现金流 */
.net-cash-flow {
  display: flex;
  align-items: center;
  gap: 12px;
}

.net-cash-flow .label {
  font-size: 14px;
  color: var(--text-tertiary);
}

.net-cash-flow .value {
  font-size: 24px;
  font-weight: 600;
}

.net-cash-flow .value.positive {
  color: #67c23a;
}

.net-cash-flow .value.negative {
  color: #f56c6c;
}

.net-cash-flow .hint {
  font-size: 12px;
  color: var(--text-tertiary);
}
</style>
