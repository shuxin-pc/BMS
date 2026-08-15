<template>
  <div class="points-management">
    <!-- Tab 切换 -->
    <el-tabs v-model="activeTab" class="points-tabs">
      <!-- ==================== 积分规则 ==================== -->
      <el-tab-pane label="积分规则" name="rule">
        <!-- 未配置提示：库中无规则时表单展示的是建议默认值，必须明确告知未生效，避免误以为规则已存在 -->
        <el-alert
          v-if="!ruleLoading && ruleForm.id === 0"
          type="warning"
          :closable="false"
          show-icon
          class="rule-unsaved-alert"
        >
          <template #title>
            <span class="alert-title">当前门店尚未配置积分规则，下方为建议默认值，尚未保存生效</span>
          </template>
          <div class="alert-content">
            未点击「保存配置」前，<strong>消费下单、储值充值、疗程卡购买均不会发放任何积分</strong>。积分规则按门店独立配置，每个门店需各自保存一次。
          </div>
        </el-alert>

        <!-- 操作栏 -->
        <div class="table-toolbar">
          <div class="toolbar-left">
            <el-button type="primary" @click="handleSaveRule" :loading="ruleSaving">
              <el-icon><Check /></el-icon>
              保存配置
            </el-button>
          </div>
        </div>

        <!-- 规则表单 -->
        <div class="card">
          <el-form
            ref="ruleFormRef"
            :model="ruleForm"
            :rules="ruleFormRules"
            label-width="160px"
            v-loading="ruleLoading"
            style="max-width: 640px; padding: 24px;"
          >
            <el-form-item label="消费1元获得积分" prop="pointsRate">
              <el-input-number
                v-model="ruleForm.pointsRate"
                :min="1"
                :step="1"
                :precision="0"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">积分</span>
            </el-form-item>
            <el-form-item label="积分抵扣1元所需积分" prop="deductPointsPerYuan">
              <el-input-number
                v-model="ruleForm.deductPointsPerYuan"
                :min="1"
                :step="1"
                :precision="0"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">积分</span>
              <div class="form-tip">消费/录入 {{ ruleForm.deductPointsPerYuan }} 积分可抵扣 1 元</div>
            </el-form-item>
            <el-form-item label="单笔最高抵扣金额" prop="maxDeductAmount">
              <el-input-number
                v-model="ruleForm.maxDeductAmount"
                :min="0"
                :step="1"
                :precision="0"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">元</span>
              <div class="form-tip">0 表示不限</div>
            </el-form-item>
            <el-form-item label="最低获取门槛" prop="minAmountThreshold">
              <el-input-number
                v-model="ruleForm.minAmountThreshold"
                :min="0"
                :step="1"
                :precision="0"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">元</span>
              <div class="form-tip">单笔金额低于此值不发放积分，0 表示无门槛；对消费下单、储值充值、疗程卡购买均生效</div>
            </el-form-item>
            <el-form-item label="积分有效期" prop="pointsValidityDays">
              <el-input-number
                v-model="ruleForm.pointsValidityDays"
                :min="1"
                :step="1"
                :precision="0"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">天</span>
              <div class="form-tip">留空表示永久</div>
            </el-form-item>
            <el-form-item label="生日双倍积分" prop="birthdayDouble">
              <el-switch v-model="ruleForm.birthdayDouble" />
              <span class="form-tip-suffix">生日当天消费获取双倍积分</span>
            </el-form-item>
            <el-form-item label="生效状态" prop="status">
              <el-radio-group v-model="ruleForm.status">
                <el-radio :value="1">启用</el-radio>
                <el-radio :value="0">停用</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="备注" prop="remark">
              <el-input v-model="ruleForm.remark" type="textarea" :rows="3" placeholder="请输入备注" />
            </el-form-item>
          </el-form>
        </div>
      </el-tab-pane>

      <!-- ==================== 积分流水 ==================== -->
      <el-tab-pane label="积分流水" name="record">
        <!-- 搜索区域 -->
        <div class="card mb-20">
          <div class="search-form">
            <el-form :inline="true" :model="recordSearchForm" class="search-form-inline">
              <el-form-item label="客户名称">
                <el-input
                  v-model="recordSearchForm.customerName"
                  placeholder="请输入客户名称"
                  clearable
                  style="width: 180px"
                />
              </el-form-item>
              <el-form-item label="手机号">
                <el-input
                  v-model="recordSearchForm.phone"
                  placeholder="请输入手机号"
                  clearable
                  style="width: 180px"
                />
              </el-form-item>
              <el-form-item label="变动类型">
                <el-select v-model="recordSearchForm.changeType" placeholder="全部" clearable style="width: 130px">
                  <el-option label="消费获取" :value="1" />
                  <el-option label="积分抵扣" :value="2" />
                  <el-option label="退款扣减" :value="3" />
                  <el-option label="充值获得" :value="5" />
                  <el-option label="疗程卡购买" :value="6" />
                  <el-option label="过期清零" :value="7" />
                  <el-option label="手动调整" :value="8" />
                </el-select>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="handleRecordSearch">
                  <el-icon><Search /></el-icon>
                  搜索
                </el-button>
                <el-button @click="handleRecordReset">
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
            <el-button circle @click="loadRecords">
              <el-icon><Refresh /></el-icon>
            </el-button>
          </div>
        </div>

        <!-- 表格区域 -->
        <div class="card">
          <el-table
            v-loading="recordLoading"
            :data="recordData"
            style="width: 100%"
          >
            <el-table-column prop="customerName" label="客户名称" width="120" />
            <el-table-column prop="phone" label="手机号" width="140" />
            <el-table-column label="积分变动" width="110" align="right">
              <template #default="{ row }">
                <span :class="row.points > 0 ? 'points-add' : 'points-sub'">
                  {{ row.points > 0 ? '+' : '' }}{{ row.points }}
                </span>
              </template>
            </el-table-column>
            <el-table-column prop="beforePoints" label="变动前积分" width="110" align="right" />
            <el-table-column label="变动后积分" width="110" align="right">
              <template #default="{ row }">
                <span class="points-balance">{{ row.afterPoints }}</span>
              </template>
            </el-table-column>
            <el-table-column label="变动类型" width="110" align="center">
              <template #default="{ row }">
                <el-tag :type="changeTypeTagType(row.type)" size="small" effect="plain">
                  {{ changeTypeText(row.type) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="变动时间" width="170">
              <template #default="{ row }">
                <span>{{ formatDateTime(row.changeTime) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="orderNo" label="关联订单" width="160" show-overflow-tooltip>
              <template #default="{ row }">
                <span v-if="row.orderNo">{{ row.orderNo }}</span>
                <span v-else class="text-muted">-</span>
              </template>
            </el-table-column>
            <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
          </el-table>

          <!-- 分页 -->
          <div class="pagination-container">
            <el-pagination
              v-model:current-page="recordPagination.pageIndex"
              v-model:page-size="recordPagination.pageSize"
              :page-sizes="systemConfigStore.defaultPageSizes"
              :total="recordPagination.total"
              layout="total, sizes, prev, pager, next, jumper"
              @size-change="loadRecords"
              @current-change="loadRecords"
            />
          </div>
        </div>

      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Check } from '@element-plus/icons-vue'
import { getPointsRule, savePointsRule, getPointsRecords } from '@/api/customer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { PointsRecord, PointsChangeType } from '@/api/customer/types'

const systemConfigStore = useSystemConfigStore()

const activeTab = ref('rule')

// ==================== 积分规则 ====================
const ruleLoading = ref(false)
const ruleSaving = ref(false)
const ruleFormRef = ref<FormInstance>()

const ruleForm = reactive({
  id: 0,                          // 0 表示新建
  pointsRate: 1,                  // 消费1元获得积分
  deductPointsPerYuan: 100,       // UI 整数字段：X 积分 = 1 元（1~10000）
  maxDeductAmount: 0,             // 单笔最高抵扣金额（0=不限）
  pointsValidityDays: null as number | null,  // null=永久
  birthdayDouble: true,
  minAmountThreshold: 0,
  status: 1,                      // 0:禁用 1:启用
  remark: ''
})

const ruleFormRules: FormRules = {
  pointsRate: [
    { required: true, message: '消费积分不能为空', trigger: 'blur' },
    { type: 'number', min: 1, message: '不能小于1', trigger: 'blur' }
  ],
  deductPointsPerYuan: [
    { required: true, message: '抵扣积分不能为空', trigger: 'blur' },
    { type: 'number', min: 1, message: '不能小于1', trigger: 'blur' }
  ]
}

const loadRule = async () => {
  ruleLoading.value = true
  try {
    const data = await getPointsRule()
    if (data) {
      ruleForm.id = data.id
      ruleForm.pointsRate = data.pointsRate
      // 后端 deductRate（小数）-> 前端整数 X = round(1/deductRate)
      ruleForm.deductPointsPerYuan = data.deductRate > 0
        ? Math.round(1 / data.deductRate)
        : 100
      ruleForm.maxDeductAmount = data.maxDeductAmount
      ruleForm.pointsValidityDays = data.pointsValidityDays ?? null
      ruleForm.birthdayDouble = data.birthdayDouble
      ruleForm.minAmountThreshold = data.minAmountThreshold ?? 0
      ruleForm.status = data.status
      ruleForm.remark = data.remark || ''
    } else {
      // 无规则：表单保持默认值，保存时走 Create
      ruleForm.id = 0
    }
  } catch (error: any) {
    ElMessage.error(error.message || '加载规则失败')
  } finally {
    ruleLoading.value = false
  }
}

const handleSaveRule = async () => {
  if (!ruleFormRef.value) return
  await ruleFormRef.value.validate(async (valid) => {
    if (valid) {
      ruleSaving.value = true
      try {
        // 前端整数 X -> 后端 deductRate = 1/X（保留 6 位小数）
        const deductRate = ruleForm.deductPointsPerYuan > 0
          ? Math.round((1 / ruleForm.deductPointsPerYuan) * 1e6) / 1e6
          : 0
        await savePointsRule({
          id: ruleForm.id,
          pointsRate: ruleForm.pointsRate,
          deductRate,
          maxDeductAmount: ruleForm.maxDeductAmount,
          pointsValidityDays: ruleForm.pointsValidityDays,
          birthdayDouble: ruleForm.birthdayDouble,
          minAmountThreshold: ruleForm.minAmountThreshold,
          status: ruleForm.status,
          remark: ruleForm.remark
        })
        ElMessage.success('保存成功')
        // 重新加载以获取后端生成的 id（新建场景）和时间戳
        await loadRule()
      } catch (error: any) {
        ElMessage.error(error.message || '保存失败')
      } finally {
        ruleSaving.value = false
      }
    }
  })
}

// ==================== 积分流水 ====================
const recordLoading = ref(false)
const recordData = ref<PointsRecord[]>([])
const recordSearchForm = reactive({
  customerName: '',
  phone: '',
  changeType: undefined as PointsChangeType | undefined
})
const recordPagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

const loadRecords = async () => {
  recordLoading.value = true
  try {
    const res = await getPointsRecords({
      customerName: recordSearchForm.customerName || undefined,
      phone: recordSearchForm.phone || undefined,
      changeType: recordSearchForm.changeType,
      pageIndex: recordPagination.pageIndex,
      pageSize: recordPagination.pageSize
    })
    recordData.value = res.list
    recordPagination.total = res.total
  } catch (error: any) {
    ElMessage.error(error.message || '加载流水失败')
  } finally {
    recordLoading.value = false
  }
}

const handleRecordSearch = () => {
  recordPagination.pageIndex = 1
  loadRecords()
}

const handleRecordReset = () => {
  recordSearchForm.customerName = ''
  recordSearchForm.phone = ''
  recordSearchForm.changeType = undefined
  handleRecordSearch()
}

// ==================== 工具方法 ====================

/** 变动类型文本 */
const changeTypeText = (type: number) => {
  const map: Record<number, string> = {
    1: '消费获取',
    2: '积分抵扣',
    3: '退款扣减',
    5: '充值获得',
    6: '疗程卡购买',
    7: '过期清零',
    8: '手动调整'
  }
  return map[type] || '未知'
}

/** 变动类型标签 */
const changeTypeTagType = (type: number) => {
  const map: Record<number, string> = {
    1: 'success',
    2: 'warning',
    3: 'danger',
    5: 'success',
    6: 'success',
    7: 'info',
    8: 'warning'
  }
  return map[type] || ''
}

/** 格式化日期时间 */
const formatDateTime = (dateStr?: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  recordPagination.pageSize = systemConfigStore.defaultPageSize
  loadRule()
  loadRecords()
})
</script>

<style scoped>
.points-management {
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

/* margin-left: auto 保证工具栏只有右侧内容时按钮仍靠右（本页规则页签只有左侧按钮，流水页签只有右侧按钮） */
.toolbar-right {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

/* 积分变动样式 */
.points-add {
  color: var(--el-color-success);
  font-weight: 600;
}

.points-sub {
  color: var(--el-color-danger);
  font-weight: 600;
}

.points-balance {
  color: var(--primary);
  font-weight: 500;
}

.text-muted {
  color: var(--text-tertiary);
}

/* 未配置规则提示 */
.rule-unsaved-alert {
  margin-bottom: 16px;
}

.rule-unsaved-alert .alert-title {
  font-size: 14px;
  font-weight: 600;
}

.rule-unsaved-alert .alert-content {
  margin-top: 4px;
  font-size: 13px;
  line-height: 1.6;
}

/* 表单提示 */
.form-tip {
  font-size: 12px;
  color: var(--text-tertiary);
  line-height: 1.5;
  margin-top: 4px;
}

.form-tip-suffix {
  margin-left: 8px;
  color: var(--text-tertiary);
  font-size: 13px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
