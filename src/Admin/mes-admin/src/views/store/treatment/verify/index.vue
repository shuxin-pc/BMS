<template>
  <div class="treatment-card-verify">
    <!-- 上方：搜索客户疗程卡 -->
    <div class="card mb-20">
      <div class="search-form">
        <div class="section-title">
          <el-icon><Search /></el-icon>
          <span>选择客户疗程卡</span>
        </div>
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称">
            <el-input
              v-model="searchForm.customerName"
              placeholder="请输入客户名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="手机号">
            <el-input
              v-model="searchForm.phone"
              placeholder="请输入手机号"
              clearable
              style="width: 150px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearchCards">
              <el-icon><Search /></el-icon>
              查询疗程卡
            </el-button>
            <el-button @click="handleResetCards">
              <el-icon><Refresh /></el-icon>
              重置
            </el-button>
          </el-form-item>
        </el-form>
      </div>

      <!-- 客户疗程卡列表 -->
      <div v-if="customerCards.length > 0" class="card-list">
        <el-table
          :data="customerCards"
          v-loading="cardLoading"
          highlight-current-row
          @current-change="handleCardSelect"
          style="width: 100%"
        >
          <el-table-column prop="saleNo" label="销售单号" width="150" />
          <el-table-column prop="customerName" label="客户名称" width="100" />
          <el-table-column prop="phone" label="手机号" width="130" />
          <el-table-column prop="cardName" label="卡名称" min-width="140" />
          <el-table-column label="剩余次数" width="100" align="center">
            <template #default="{ row }">
              <span :class="{ 'count-warn': row.remainingTimes <= 2 }">{{ row.remainingTimes }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="saleTime" label="购买时间" width="170" />
          <el-table-column label="状态" width="90">
            <template #default="{ row }">
              <el-tag :type="getSaleStatusType(row.status)" size="small" effect="dark">
                {{ getSaleStatusText(row.status) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120" fixed="right">
            <template #default="{ row }">
              <el-button
                type="primary"
                size="small"
                :disabled="row.remainingTimes <= 0"
                @click.stop="handleVerify(row)"
              >
                <el-icon><Check /></el-icon>
                核销
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
      <div v-else-if="hasSearched" class="empty-hint">
        <el-icon><WarningFilled /></el-icon>
        <span>未查询到有效的疗程卡</span>
      </div>
    </div>

    <!-- 下方：核销记录列表 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <span class="toolbar-hint">核销记录</span>
      </div>
      <div class="toolbar-right">
        <el-button circle @click="loadVerifyRecords">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <div class="card">
      <el-table
        v-loading="recordLoading"
        :data="verifyRecords"
        style="width: 100%"
      >
        <el-table-column prop="verifyNo" label="核销单号" width="150" />
        <el-table-column prop="customerName" label="客户名称" width="100" />
        <el-table-column prop="cardName" label="卡名称" min-width="140" show-overflow-tooltip />
        <el-table-column prop="verifyItem" label="核销项目" width="140" show-overflow-tooltip />
        <el-table-column prop="verifyTime" label="核销时间" width="170" />
        <el-table-column label="剩余次数" width="100" align="center">
          <template #default="{ row }">
            <span :class="{ 'count-warn': row.remainingCount <= 2 }">{{ row.remainingCount }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="100" />
      </el-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="pagination.pageIndex"
          v-model:page-size="pagination.pageSize"
          :page-sizes="systemConfigStore.defaultPageSizes"
          :total="pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadVerifyRecords"
          @current-change="loadVerifyRecords"
        />
      </div>
    </div>

    <!-- 核销确认弹窗 -->
    <el-dialog v-model="verifyDialogVisible" title="疗程卡核销" width="720px" :close-on-click-modal="false">
      <el-descriptions :column="1" border v-if="selectedCard">
        <el-descriptions-item label="客户名称">{{ selectedCard.customerName }}</el-descriptions-item>
        <el-descriptions-item label="卡名称">{{ selectedCard.cardName }}</el-descriptions-item>
        <el-descriptions-item label="剩余次数">{{ selectedCard.remainingTimes }} 次</el-descriptions-item>
      </el-descriptions>
      <el-form
        ref="verifyFormRef"
        :model="verifyForm"
        :rules="verifyRules"
        label-width="100px"
        class="verify-form"
      >
        <div class="verify-items-header">
          <span class="items-label">核销项目（至少 1 项）</span>
          <el-button type="primary" link :disabled="cardItems.length === 0" @click="addVerifyItem">
            <el-icon><Plus /></el-icon>
            添加项目
          </el-button>
        </div>
        <el-table :data="verifyForm.items" border class="verify-items-table">
          <el-table-column label="序号" type="index" width="55" align="center" />
          <el-table-column label="项目" min-width="240">
            <template #default="{ row, $index }">
              <el-form-item
                :prop="`items.${$index}.productId`"
                :rules="[{ required: true, message: '请选择项目', trigger: 'change' }]"
                :show-message="false"
                style="margin-bottom: 0"
              >
                <el-select
                  v-model="row.productId"
                  placeholder="请选择核销项目"
                  style="width: 100%"
                  @change="recalcItemAmount($index)"
                >
                  <el-option
                    v-for="item in cardItems"
                    :key="item.productId"
                    :label="`项目 #${item.productId}（折算单价 ¥${item.allocatedUnitPrice}）`"
                    :value="item.productId"
                  />
                </el-select>
              </el-form-item>
            </template>
          </el-table-column>
          <el-table-column label="次数" width="130" align="center">
            <template #default="{ row, $index }">
              <el-form-item
                :prop="`items.${$index}.verifyTimes`"
                :rules="verifyTimesRulesFor($index)"
                :show-message="false"
                style="margin-bottom: 0"
              >
                <el-input-number v-model="row.verifyTimes" :min="1" style="width: 100%" @change="recalcItemAmount($index)" />
              </el-form-item>
            </template>
          </el-table-column>
          <el-table-column label="金额(元)" width="110" align="right">
            <template #default="{ row }">
              <span class="amount-text">¥{{ formatAmount(calcItemAmount(row)) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="70" align="center" fixed="right">
            <template #default="{ $index }">
              <el-button type="danger" link :disabled="verifyForm.items.length === 1" @click="removeVerifyItem($index)">
                <el-icon><Delete /></el-icon>
              </el-button>
            </template>
          </el-table-column>
        </el-table>
        <div class="verify-totals">
          <span>合计：<b class="total-amount">¥{{ formatAmount(totalAmount) }}</b></span>
          <span class="total-times">共 {{ totalTimes }} 次</span>
        </div>
        <el-form-item label="备注" prop="remark" style="margin-top: 16px">
          <el-input v-model="verifyForm.remark" type="textarea" :rows="2" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="verifyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="verifyLoading" @click="handleSubmitVerify">
          确认核销
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Check, WarningFilled, Plus, Delete } from '@element-plus/icons-vue'
import {
  getCustomerTreatmentCards,
  getTreatmentCardConfig,
  getTreatmentCardVerifies,
  verifyTreatmentCard
} from '@/api/treatment-card'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type {
  CourseCardItem,
  TreatmentCardSale,
  TreatmentCardVerify,
  TreatmentCardVerifyItemInput
} from '@/api/treatment-card/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerName: '',
  phone: ''
})

// 客户疗程卡
const cardLoading = ref(false)
const customerCards = ref<TreatmentCardSale[]>([])
const hasSearched = ref(false)
const selectedCard = ref<TreatmentCardSale | null>(null)

// 核销记录
const recordLoading = ref(false)
const verifyRecords = ref<TreatmentCardVerify[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 销售状态文本
const getSaleStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '有效', 2: '已用完', 3: '已退款' }
  return map[status] || '未知'
}

// 销售状态标签类型
const getSaleStatusType = (status: number): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: 'success',
    2: 'info',
    3: 'danger'
  }
  return map[status] || 'info'
}

// 搜索客户疗程卡
const handleSearchCards = async () => {
  if (!searchForm.customerName && !searchForm.phone) {
    ElMessage.warning('请输入客户名称或手机号进行查询')
    return
  }
  cardLoading.value = true
  hasSearched.value = true
  try {
    customerCards.value = await getCustomerTreatmentCards(
      searchForm.customerName || undefined,
      searchForm.phone || undefined
    )
    if (customerCards.value.length === 0) {
      ElMessage.info('未找到有效的疗程卡')
    }
  } catch (error) {
    ElMessage.error('查询失败')
  } finally {
    cardLoading.value = false
  }
}

// 重置搜索
const handleResetCards = () => {
  searchForm.customerName = ''
  searchForm.phone = ''
  customerCards.value = []
  hasSearched.value = false
  selectedCard.value = null
}

// 选择疗程卡
const handleCardSelect = (row: TreatmentCardSale | null) => {
  selectedCard.value = row
}

// 加载核销记录
const loadVerifyRecords = async () => {
  recordLoading.value = true
  try {
    const res = await getTreatmentCardVerifies({
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    verifyRecords.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error('加载核销记录失败')
  } finally {
    recordLoading.value = false
  }
}

// 核销弹窗
const verifyDialogVisible = ref(false)
const verifyLoading = ref(false)
const verifyFormRef = ref<FormInstance>()
const cardItems = ref<CourseCardItem[]>([])
const itemsLoading = ref(false)

const verifyForm = reactive<{
  items: TreatmentCardVerifyItemInput[]
  remark: string
}>({
  items: [{ productId: 0, verifyTimes: 1 }],
  remark: ''
})

const verifyRules: FormRules = {
  // items 数组的逐行规则通过 :prop 在 el-form-item 上声明
}

// 核销次数行内验证器（按行索引动态生成）
const verifyTimesRulesFor = (index: number) => [
  { required: true, message: '次数必填', trigger: 'blur' },
  {
    validator: (_rule: unknown, value: number, callback: (err?: Error) => void) => {
      if (!value || value < 1) {
        callback(new Error('至少 1 次'))
      } else if (value + getUsedTimesExcluding(index) > (selectedCard.value?.remainingTimes ?? 0)) {
        callback(new Error(`总次数不能超过 ${selectedCard.value?.remainingTimes}`))
      } else {
        callback()
      }
    },
    trigger: 'blur'
  }
]

// 创建一行空白的核销项目
const createEmptyItem = (): TreatmentCardVerifyItemInput => ({
  productId: 0,
  verifyTimes: 1
})

// 添加核销项目行
const addVerifyItem = () => {
  verifyForm.items.push(createEmptyItem())
}

// 移除核销项目行
const removeVerifyItem = (index: number) => {
  if (verifyForm.items.length > 1) {
    verifyForm.items.splice(index, 1)
  }
}

// 根据项目 ID 查找折算单价
const findUnitPrice = (productId: number | undefined): number => {
  if (productId == null) return 0
  const item = cardItems.value.find(i => i.productId === productId)
  return item ? Number(item.allocatedUnitPrice) : 0
}

// 计算单行金额（前端展示用，实际金额由后端计算）
const calcItemAmount = (row: TreatmentCardVerifyItemInput): number => {
  return Number((findUnitPrice(row.productId) * (row.verifyTimes || 0)).toFixed(2))
}

// 切换项目/次数时无需做额外动作（金额已用 computed 派生），保留占位方法便于模板调用
const recalcItemAmount = (_index: number) => {
  // 金额由 calcItemAmount 实时计算，无需手动同步
}

// 获取除指定行外的总次数（用于"总次数不能超过剩余次数"校验）
const getUsedTimesExcluding = (excludeIndex: number): number => {
  return verifyForm.items.reduce((sum, item, idx) => {
    if (idx === excludeIndex) return sum
    return sum + (item.verifyTimes || 0)
  }, 0)
}

// 合计金额与总次数
const totalAmount = computed(() => {
  return Number(
    verifyForm.items
      .reduce((sum, item) => sum + calcItemAmount(item), 0)
      .toFixed(2)
  )
})

const totalTimes = computed(() => {
  return verifyForm.items.reduce((sum, item) => sum + (item.verifyTimes || 0), 0)
})

// 金额格式化
const formatAmount = (val: number): string => {
  return (val ?? 0).toFixed(2)
}

// 打开核销弹窗，加载疗程卡项目列表
const handleVerify = async (row: TreatmentCardSale) => {
  selectedCard.value = row
  // 重置表单：默认 1 行空项目，备注清空
  verifyForm.items = [createEmptyItem()]
  verifyForm.remark = ''
  cardItems.value = []
  verifyDialogVisible.value = true

  // 加载疗程卡配置的项目列表（核销项目下拉数据源）
  if (row.cardId) {
    itemsLoading.value = true
    try {
      const config = await getTreatmentCardConfig(row.cardId)
      cardItems.value = config.items || []
    } catch (error) {
      ElMessage.error('加载疗程卡项目失败')
    } finally {
      itemsLoading.value = false
    }
  }
}

// 提交核销
const handleSubmitVerify = async () => {
  if (!verifyFormRef.value || !selectedCard.value) return
  // 提前保存 ID，避免异步闭包内 selectedCard.value 被置空导致空指针
  const cardSaleId = selectedCard.value.id
  await verifyFormRef.value.validate(async (valid) => {
    if (valid) {
      // 二次校验：总次数不能超过剩余次数
      if (totalTimes.value > (selectedCard.value?.remainingTimes ?? 0)) {
        ElMessage.error(`总次数 ${totalTimes.value} 超过剩余次数 ${selectedCard.value?.remainingTimes}`)
        return
      }
      verifyLoading.value = true
      try {
        await verifyTreatmentCard({
          cardSaleId,
          items: verifyForm.items
            .filter((i): i is { productId: number; verifyTimes: number } => i.productId != null && i.productId > 0 && i.verifyTimes > 0)
            .map(i => ({ productId: i.productId, verifyTimes: i.verifyTimes })),
          remark: verifyForm.remark || undefined
        })
        ElMessage.success('核销成功')
        verifyDialogVisible.value = false
        // 刷新数据
        handleSearchCards()
        loadVerifyRecords()
      } catch (error: any) {
        ElMessage.error(error.message || '核销失败')
      } finally {
        verifyLoading.value = false
      }
    }
  })
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadVerifyRecords()
})
</script>

<style scoped>
.treatment-card-verify {
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

.section-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 16px;
}

.card-list {
  padding: 0 24px 20px;
}

.empty-hint {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px 0;
  color: var(--text-tertiary);
  font-size: 14px;
}

/* 操作栏 */
.table-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding: 0 4px;
}

.toolbar-left {
  display: flex;
  gap: 12px;
  align-items: center;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

.toolbar-hint {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.count-text {
  color: var(--text-primary);
}

.count-warn {
  color: #e6a23c;
  font-weight: 600;
}

.verify-form {
  margin-top: 20px;
}

.form-hint {
  margin-left: 8px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.verify-items-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.items-label {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.verify-items-table {
  margin-bottom: 12px;
}

.verify-totals {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  font-size: 14px;
  color: var(--text-primary);
}

.total-amount {
  color: var(--el-color-primary);
  font-size: 16px;
  margin-left: 4px;
}

.total-times {
  color: var(--text-tertiary);
  font-size: 13px;
}

.amount-text {
  font-weight: 600;
  color: var(--text-primary);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
