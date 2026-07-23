<template>
  <div class="treatment-transfer">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="转让日期">
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
          <el-form-item label="客户名称">
            <el-input
              v-model="searchForm.customerName"
              placeholder="原客户/新客户"
              clearable
              style="width: 160px"
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
      <div class="toolbar-left">
        <span class="toolbar-hint">疗程卡转让记录</span>
      </div>
      <div class="toolbar-right">
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新增转让
        </el-button>
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
        <el-table-column prop="transferDate" label="转让日期" width="120" />
        <el-table-column prop="saleNo" label="销售单号" width="150" />
        <el-table-column prop="cardName" label="疗程卡名称" min-width="140" show-overflow-tooltip />
        <el-table-column label="原客户" width="140">
          <template #default="{ row }">
            <div class="customer-cell">
              <span class="customer-name">{{ row.fromCustomerName }}</span>
              <span class="customer-phone">{{ row.fromCustomerPhone }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="" width="40" align="center">
          <template #default>
            <el-icon class="transfer-arrow"><Right /></el-icon>
          </template>
        </el-table-column>
        <el-table-column label="新客户" width="140">
          <template #default="{ row }">
            <div class="customer-cell">
              <span class="customer-name">{{ row.toCustomerName }}</span>
              <span class="customer-phone">{{ row.toCustomerPhone }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="转让手续费" width="110" align="right">
          <template #default="{ row }">
            <span class="fee-text">¥{{ formatPrice(row.transferFee) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="storeName" label="操作门店" width="100" />
        <el-table-column prop="operatorName" label="操作员" width="100" />
        <el-table-column label="状态" width="90" align="center">
          <template #default>
            <el-tag type="success" size="small" effect="dark">
              已转让
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
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

    <!-- 新增转让弹窗 -->
    <el-dialog v-model="dialogVisible" title="新增疗程卡转让" width="560px" @closed="handleDialogClosed">
      <el-form
        ref="formRef"
        :model="transferForm"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="疗程卡" prop="cardSaleId">
          <el-select
            v-model="transferForm.cardSaleId"
            placeholder="请选择疗程卡销售记录"
            filterable
            style="width: 100%"
            @change="handleCardSaleChange"
          >
            <el-option
              v-for="item in cardSaleOptions"
              :key="item.id"
              :label="`${item.saleNo} - ${item.cardName}（${item.customerName}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="原客户">
          <el-input
            :model-value="fromCustomerDisplay"
            disabled
            placeholder="选择疗程卡后自动带出"
          />
        </el-form-item>

        <el-form-item label="剩余次数">
          <el-input
            :model-value="remainingDisplay"
            disabled
          />
        </el-form-item>

        <el-form-item label="新客户" prop="toCustomerId">
          <el-select
            v-model="transferForm.toCustomerId"
            placeholder="请选择新客户"
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="item in availableToCustomers"
              :key="item.id"
              :label="`${item.name}（${item.phone}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="转让日期" prop="transferDate">
          <el-date-picker
            v-model="transferForm.transferDate"
            type="date"
            placeholder="请选择转让日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="转让手续费" prop="transferFee">
          <el-input-number
            v-model="transferForm.transferFee"
            :min="0"
            :precision="2"
            :step="10"
            style="width: 100%"
          />
          <span class="form-hint">元</span>
        </el-form-item>

        <el-form-item label="备注">
          <el-input
            v-model="transferForm.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入备注信息"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确认转让
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Right } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import {
  getTreatmentCardTransfers,
  getTransferableCardSales,
  getCustomerOptions,
  createTreatmentCardTransfer
} from '@/api/treatment-transfer'
import type {
  TreatmentCardTransfer,
  TreatmentCardTransferCreate,
  CardSaleOption,
  CustomerOption
} from '@/api/treatment-transfer/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  dateRange: [] as string[],
  customerName: ''
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<TreatmentCardTransfer[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 格式化价格
const formatPrice = (price: number) => price.toFixed(2)

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTreatmentCardTransfers({
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
      customerName: searchForm.customerName || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error('加载数据失败')
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
  searchForm.dateRange = []
  searchForm.customerName = ''
  handleSearch()
}

// ==================== 新增转让弹窗 ====================

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const transferForm = reactive<TreatmentCardTransferCreate>({
  cardSaleId: 0,
  toCustomerId: 0,
  transferDate: new Date().toISOString().substring(0, 10),
  transferFee: 0,
  remark: ''
})

const formRules: FormRules = {
  cardSaleId: [{ required: true, message: '请选择疗程卡', trigger: 'change' }],
  toCustomerId: [{ required: true, message: '请选择新客户', trigger: 'change' }],
  transferDate: [{ required: true, message: '请选择转让日期', trigger: 'change' }],
  transferFee: [{ required: true, message: '请输入转让手续费', trigger: 'blur' }]
}

// 可选疗程卡列表
const cardSaleOptions = ref<CardSaleOption[]>([])
// 客户列表
const customerOptions = ref<CustomerOption[]>([])
// 当前选中的疗程卡销售记录
const selectedCardSale = ref<CardSaleOption | null>(null)

// 原客户展示文本
const fromCustomerDisplay = computed(() => {
  if (!selectedCardSale.value) return ''
  return `${selectedCardSale.value.customerName}（${selectedCardSale.value.customerPhone}）`
})

// 剩余次数展示
const remainingDisplay = computed(() => {
  if (!selectedCardSale.value) return ''
  return `${selectedCardSale.value.remainingCount} / ${selectedCardSale.value.totalCount} 次`
})

// 可选新客户列表（排除原客户）
const availableToCustomers = computed(() => {
  const fromId = selectedCardSale.value?.customerId
  return customerOptions.value.filter(c => c.id !== fromId)
})

// 疗程卡选择变化
const handleCardSaleChange = (id: number) => {
  selectedCardSale.value = cardSaleOptions.value.find(c => c.id === id) || null
  // 切换疗程卡时清空新客户选择（如果新客户是原客户）
  if (selectedCardSale.value && transferForm.toCustomerId === selectedCardSale.value.customerId) {
    transferForm.toCustomerId = 0
  }
}

// 打开新增弹窗
const handleAdd = async () => {
  dialogVisible.value = true
  // 加载选项数据
  if (cardSaleOptions.value.length === 0) {
    cardSaleOptions.value = await getTransferableCardSales()
  }
  if (customerOptions.value.length === 0) {
    customerOptions.value = await getCustomerOptions()
  }
}

// 弹窗关闭后重置表单
const handleDialogClosed = () => {
  formRef.value?.resetFields()
  selectedCardSale.value = null
  transferForm.cardSaleId = 0
  transferForm.toCustomerId = 0
  transferForm.transferDate = new Date().toISOString().substring(0, 10)
  transferForm.transferFee = 0
  transferForm.remark = ''
}

// 提交转让
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      await createTreatmentCardTransfer({
        cardSaleId: transferForm.cardSaleId,
        toCustomerId: transferForm.toCustomerId,
        transferDate: transferForm.transferDate,
        transferFee: transferForm.transferFee,
        remark: transferForm.remark || undefined
      })
      ElMessage.success('疗程卡转让成功')
      dialogVisible.value = false
      // 刷新选项数据（因为转让后卡的客户信息已变化）
      cardSaleOptions.value = []
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '转让失败')
    } finally {
      submitLoading.value = false
    }
  })
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadData()
})
</script>

<style scoped>
.treatment-transfer {
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

.search-form {
  padding: 20px 24px 0;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

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
  font-size: 13px;
  color: var(--text-tertiary);
}

.customer-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.customer-name {
  color: var(--text-primary);
  font-size: 13px;
}

.customer-phone {
  color: var(--text-tertiary);
  font-size: 12px;
}

.transfer-arrow {
  color: var(--primary);
  font-size: 16px;
}

.fee-text {
  color: var(--warning);
  font-weight: 600;
}

.form-hint {
  margin-left: 8px;
  color: var(--text-tertiary);
  font-size: 13px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
