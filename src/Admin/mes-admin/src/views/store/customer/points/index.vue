<template>
  <div class="points-management">
    <!-- Tab 切换 -->
    <el-tabs v-model="activeTab" class="points-tabs">
      <!-- ==================== 积分规则 ==================== -->
      <el-tab-pane label="积分规则" name="rule">
        <!-- 操作栏 -->
        <div class="table-toolbar">
          <div class="toolbar-left">
            <span class="toolbar-title">积分规则配置</span>
          </div>
          <div class="toolbar-right">
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
            <el-form-item label="规则名称" prop="name">
              <el-input v-model="ruleForm.name" placeholder="请输入规则名称" />
            </el-form-item>
            <el-form-item label="消费1元获得积分" prop="pointsPerYuan">
              <el-input-number
                v-model="ruleForm.pointsPerYuan"
                :min="0"
                :step="1"
                :precision="2"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">积分</span>
            </el-form-item>
            <el-form-item label="积分抵扣比例" prop="pointsToYuan">
              <el-input-number
                v-model="ruleForm.pointsToYuan"
                :min="0"
                :step="0.001"
                :precision="3"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">元/积分</span>
              <div class="form-tip">如 0.01 表示 100 积分 = 1 元</div>
            </el-form-item>
            <el-form-item label="生日双倍积分" prop="birthdayDouble">
              <el-switch v-model="ruleForm.birthdayDouble" />
              <span class="form-tip-suffix">生日当天消费获取双倍积分</span>
            </el-form-item>
            <el-form-item label="最低获取门槛" prop="minPointsThreshold">
              <el-input-number
                v-model="ruleForm.minPointsThreshold"
                :min="0"
                :step="1"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">积分</span>
              <div class="form-tip">单笔消费低于此值不获取积分</div>
            </el-form-item>
            <el-form-item label="生效状态" prop="status">
              <el-radio-group v-model="ruleForm.status">
                <el-radio :value="1">启用</el-radio>
                <el-radio :value="2">停用</el-radio>
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
              <el-form-item label="变动类型">
                <el-select v-model="recordSearchForm.changeType" placeholder="全部" clearable style="width: 130px">
                  <el-option label="消费获取" :value="1" />
                  <el-option label="兑换扣减" :value="2" />
                  <el-option label="活动赠送" :value="3" />
                  <el-option label="退款扣减" :value="4" />
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
          <div class="toolbar-left">
            <span class="toolbar-title">积分流水列表</span>
          </div>
          <div class="toolbar-right">
            <el-button type="warning" @click="openExchangeDialog">
              <el-icon><Exchange /></el-icon>
              积分兑换
            </el-button>
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
                <span :class="row.changePoints > 0 ? 'points-add' : 'points-sub'">
                  {{ row.changePoints > 0 ? '+' : '' }}{{ row.changePoints }}
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
                <el-tag :type="changeTypeTagType(row.changeType)" size="small" effect="plain">
                  {{ changeTypeText(row.changeType) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="changeTime" label="变动时间" width="170" />
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

        <!-- 积分兑换对话框 -->
        <el-dialog v-model="exchangeDialogVisible" title="积分兑换" width="480px">
          <el-form :model="exchangeForm" label-width="100px">
            <el-form-item label="客户" required>
              <el-select v-model="exchangeForm.customerId" placeholder="请选择客户" style="width: 100%" filterable>
                <el-option
                  v-for="item in exchangeCustomerList"
                  :key="item.customerId"
                  :label="`${item.customerName}（${item.phone}）- 当前积分：${item.currentPoints}`"
                  :value="item.customerId"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="兑换积分" required>
              <el-input-number
                v-model="exchangeForm.points"
                :min="1"
                :step="10"
                controls-position="right"
                style="width: 200px"
              />
              <span class="form-tip-suffix">积分</span>
            </el-form-item>
            <el-form-item label="备注">
              <el-input v-model="exchangeForm.remark" type="textarea" :rows="2" placeholder="请输入备注（选填）" />
            </el-form-item>
          </el-form>
          <template #footer>
            <el-button @click="exchangeDialogVisible = false">取消</el-button>
            <el-button type="primary" @click="handleExchange" :loading="exchangeSubmitting">确认兑换</el-button>
          </template>
        </el-dialog>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Check, Exchange } from '@element-plus/icons-vue'
import { getPointsRule, savePointsRule, getPointsRecords, exchangePoints, getCustomersForPointsExchange } from '@/api/customer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { PointsRecord, PointsChangeType } from '@/api/customer/types'

const systemConfigStore = useSystemConfigStore()

const activeTab = ref('rule')

// ==================== 积分规则 ====================
const ruleLoading = ref(false)
const ruleSaving = ref(false)
const ruleFormRef = ref<FormInstance>()

const ruleForm = reactive({
  id: 1,
  name: '',
  pointsPerYuan: 1,
  pointsToYuan: 0.01,
  birthdayDouble: true,
  minPointsThreshold: 0,
  status: 1,
  remark: ''
})

const ruleFormRules: FormRules = {
  name: [
    { required: true, message: '规则名称不能为空', trigger: 'blur' },
    { max: 100, message: '规则名称最多100个字符', trigger: 'blur' }
  ],
  pointsPerYuan: [
    { required: true, message: '消费积分不能为空', trigger: 'blur' },
    { type: 'number', min: 0, message: '不能小于0', trigger: 'blur' }
  ],
  pointsToYuan: [
    { required: true, message: '抵扣比例不能为空', trigger: 'blur' },
    { type: 'number', min: 0, message: '不能小于0', trigger: 'blur' }
  ]
}

const loadRule = async () => {
  ruleLoading.value = true
  try {
    const data = await getPointsRule()
    ruleForm.id = data.id
    ruleForm.name = data.name
    ruleForm.pointsPerYuan = data.pointsPerYuan
    ruleForm.pointsToYuan = data.pointsToYuan
    ruleForm.birthdayDouble = data.birthdayDouble
    ruleForm.minPointsThreshold = data.minPointsThreshold
    ruleForm.status = data.status
    ruleForm.remark = data.remark || ''
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
        await savePointsRule({
          ...ruleForm,
          updatedAt: ''
        })
        ElMessage.success('保存成功')
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
  recordSearchForm.changeType = undefined
  handleRecordSearch()
}

// ==================== 积分兑换 ====================
const exchangeDialogVisible = ref(false)
const exchangeSubmitting = ref(false)
const exchangeCustomerList = ref<Array<{
  customerId: number
  customerName: string
  phone: string
  currentPoints: number
}>>([])
const exchangeForm = reactive({
  customerId: undefined as number | undefined,
  points: 0,
  remark: ''
})

/** 打开积分兑换对话框 */
const openExchangeDialog = async () => {
  exchangeDialogVisible.value = true
  exchangeForm.customerId = undefined
  exchangeForm.points = 0
  exchangeForm.remark = ''
  try {
    exchangeCustomerList.value = await getCustomersForPointsExchange()
  } catch (error: any) {
    ElMessage.error(error.message || '加载客户列表失败')
  }
}

/** 确认积分兑换 */
const handleExchange = async () => {
  if (!exchangeForm.customerId) {
    ElMessage.warning('请选择客户')
    return
  }
  if (exchangeForm.points <= 0) {
    ElMessage.warning('兑换积分必须大于0')
    return
  }
  exchangeSubmitting.value = true
  try {
    await exchangePoints(exchangeForm.customerId, exchangeForm.points, exchangeForm.remark || undefined)
    ElMessage.success('兑换成功')
    exchangeDialogVisible.value = false
    loadRecords()
  } catch (error: any) {
    // 积分不足时显示友好错误提示（后端/Mock 返回的错误消息已包含当前积分）
    ElMessage.error(error.message || '兑换失败')
  } finally {
    exchangeSubmitting.value = false
  }
}

// ==================== 工具方法 ====================

/** 变动类型文本 */
const changeTypeText = (type: number) => {
  const map: Record<number, string> = { 1: '消费获取', 2: '兑换扣减', 3: '活动赠送', 4: '退款扣减' }
  return map[type] || '未知'
}

/** 变动类型标签 */
const changeTypeTagType = (type: number) => {
  const map: Record<number, string> = { 1: 'success', 2: 'warning', 3: '', 4: 'danger' }
  return map[type] || ''
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
