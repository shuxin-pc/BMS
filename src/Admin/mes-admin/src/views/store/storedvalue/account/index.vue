<template>
  <div class="account-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
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
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="账户状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="正常" :value="1" />
              <el-option label="冻结" :value="2" />
            </el-select>
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
        <span class="toolbar-title">储值账户列表</span>
      </div>
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
        <el-table-column prop="customerName" label="客户名称" width="120" />
        <el-table-column prop="phone" label="手机号" width="140" />
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
        <el-table-column label="账户状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
              {{ row.status === 1 ? '正常' : '冻结' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="openTime" label="开卡时间" width="170" />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="row.status === 1"
              link
              type="primary"
              size="small"
              @click="handleRecharge(row)"
            >
              <el-icon><Wallet /></el-icon>
              充值
            </el-button>
            <el-button
              link
              :type="row.status === 1 ? 'danger' : 'success'"
              size="small"
              @click="handleToggleStatus(row)"
            >
              <el-icon><Lock v-if="row.status === 1" /><Unlock v-else /></el-icon>
              {{ row.status === 1 ? '冻结' : '解冻' }}
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

    <!-- 充值弹窗 -->
    <el-dialog
      v-model="rechargeVisible"
      title="储值充值"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="rechargeFormRef"
        :model="rechargeForm"
        :rules="rechargeRules"
        label-width="100px"
      >
        <el-form-item label="客户">
          <span class="customer-name">{{ rechargeForm.customerName }}</span>
        </el-form-item>
        <el-form-item label="当前余额">
          <span class="balance-text">¥{{ formatPrice(rechargeForm.currentBalance) }}</span>
        </el-form-item>
        <el-form-item label="充值金额" prop="amount">
          <el-input-number
            v-model="rechargeForm.amount"
            :min="0.01"
            :precision="2"
            :step="100"
            controls-position="right"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="赠送金额" prop="bonusAmount">
          <el-input-number
            v-model="rechargeForm.bonusAmount"
            :min="0"
            :precision="2"
            :step="10"
            controls-position="right"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="支付方式" prop="paymentMethod">
          <el-radio-group v-model="rechargeForm.paymentMethod">
            <el-radio :value="1">现金</el-radio>
            <el-radio :value="2">微信</el-radio>
            <el-radio :value="3">支付宝</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="充值后余额">
          <span class="price-text">¥{{ formatPrice(rechargeForm.currentBalance + rechargeForm.amount + rechargeForm.bonusAmount) }}</span>
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="rechargeForm.remark" type="textarea" :rows="2" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rechargeVisible = false">取消</el-button>
        <el-button type="primary" :loading="rechargeLoading" @click="handleRechargeSubmit">
          确认充值
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Wallet, Lock, Unlock } from '@element-plus/icons-vue'
import { getMemberAccounts, rechargeAccount } from '@/api/member'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { MemberAccount } from '@/api/member/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerName: '',
  phone: '',
  status: undefined as number | undefined
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
      customerName: searchForm.customerName || undefined,
      phone: searchForm.phone || undefined,
      status: searchForm.status,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error: any) {
    ElMessage.error(error.message || '加载数据失败')
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
  searchForm.customerName = ''
  searchForm.phone = ''
  searchForm.status = undefined
  handleSearch()
}

// ==================== 充值 ====================
const rechargeVisible = ref(false)
const rechargeLoading = ref(false)
const rechargeFormRef = ref<FormInstance>()

const rechargeForm = reactive({
  accountId: 0,
  customerName: '',
  currentBalance: 0,
  amount: 100,
  bonusAmount: 0,
  paymentMethod: 2,
  remark: ''
})

const rechargeRules: FormRules = {
  amount: [
    { required: true, message: '充值金额不能为空', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '充值金额必须大于0', trigger: 'blur' }
  ],
  bonusAmount: [
    { type: 'number', min: 0, message: '赠送金额不能小于0', trigger: 'blur' }
  ],
  paymentMethod: [
    { required: true, message: '请选择支付方式', trigger: 'change' }
  ]
}

const handleRecharge = (row: MemberAccount) => {
  rechargeForm.accountId = row.id
  rechargeForm.customerName = row.customerName || ''
  rechargeForm.currentBalance = row.balance
  rechargeForm.amount = 100
  rechargeForm.bonusAmount = 0
  rechargeForm.paymentMethod = 2
  rechargeForm.remark = ''
  rechargeVisible.value = true
}

const handleRechargeSubmit = async () => {
  if (!rechargeFormRef.value) return
  await rechargeFormRef.value.validate(async (valid) => {
    if (valid) {
      rechargeLoading.value = true
      try {
        await rechargeAccount({
          accountId: rechargeForm.accountId,
          amount: rechargeForm.amount,
          bonusAmount: rechargeForm.bonusAmount,
          paymentMethod: rechargeForm.paymentMethod,
          remark: rechargeForm.remark || undefined
        })
        ElMessage.success('充值成功')
        rechargeVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '充值失败')
      } finally {
        rechargeLoading.value = false
      }
    }
  })
}

// ==================== 冻结/解冻 ====================
const handleToggleStatus = (row: MemberAccount) => {
  const action = row.status === 1 ? '冻结' : '解冻'
  ElMessage.info(`${action}功能开发中，敬请期待`)
}

// ==================== 工具方法 ====================

/** 格式化价格 */
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
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

.toolbar-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
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

.price-text {
  color: var(--primary);
  font-weight: 600;
}

.consume-text {
  color: var(--el-color-danger);
}

.bonus-text {
  color: var(--el-color-success);
}

.customer-name {
  font-weight: 500;
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
