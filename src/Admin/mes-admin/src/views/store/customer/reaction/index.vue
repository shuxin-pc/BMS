<template>
  <div class="service-reaction">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称">
            <el-input
              v-model="searchForm.customerName"
              placeholder="请输入客户名称"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="反应日期">
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
          <el-form-item label="严重程度">
            <el-select v-model="searchForm.severity" placeholder="全部" clearable style="width: 120px">
              <el-option label="轻微" :value="1" />
              <el-option label="中等" :value="2" />
              <el-option label="严重" :value="3" />
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
        <span class="toolbar-hint">服务反应记录</span>
      </div>
      <div class="toolbar-right">
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新增记录
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
        <el-table-column prop="reactionDate" label="反应日期" width="120" />
        <el-table-column prop="customerName" label="客户名称" width="100" />
        <el-table-column prop="customerPhone" label="手机号" width="130" />
        <el-table-column prop="serviceItem" label="服务项目" min-width="140" show-overflow-tooltip />
        <el-table-column prop="reaction" label="反应描述" min-width="200" show-overflow-tooltip />
        <el-table-column label="严重程度" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.severity" :type="getSeverityTagType(row.severity)" size="small" effect="dark">
              {{ getSeverityText(row.severity) }}
            </el-tag>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="orderNo" label="关联订单" width="140" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.orderNo || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">
              <el-icon><View /></el-icon>
              详情
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

    <!-- 新增弹窗 -->
    <el-dialog v-model="dialogVisible" title="新增服务反应记录" width="560px" @closed="handleDialogClosed">
      <el-form
        ref="formRef"
        :model="reactionForm"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="客户" prop="customerId">
          <el-select
            v-model="reactionForm.customerId"
            placeholder="请选择客户"
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="item in customerOptions"
              :key="item.id"
              :label="`${item.name}（${item.phone}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="服务项目">
          <el-input
            v-model="reactionForm.serviceItem"
            placeholder="请输入服务项目名称"
            maxlength="100"
          />
        </el-form-item>

        <el-form-item label="反应日期" prop="reactionDate">
          <el-date-picker
            v-model="reactionForm.reactionDate"
            type="date"
            placeholder="请选择反应日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="反应描述">
          <el-input
            v-model="reactionForm.reaction"
            type="textarea"
            :rows="3"
            placeholder="请描述服务后的反应（如红肿、不适等）"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>

        <el-form-item label="严重程度">
          <el-radio-group v-model="reactionForm.severity">
            <el-radio-button :value="1">轻微</el-radio-button>
            <el-radio-button :value="2">中等</el-radio-button>
            <el-radio-button :value="3">严重</el-radio-button>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="备注">
          <el-input
            v-model="reactionForm.remark"
            type="textarea"
            :rows="2"
            placeholder="请输入备注"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          保存
        </el-button>
      </template>
    </el-dialog>

    <!-- 详情弹窗 -->
    <el-dialog v-model="detailVisible" title="反应记录详情" width="560px">
      <el-descriptions :column="2" border v-if="detailData">
        <el-descriptions-item label="反应日期">{{ detailData.reactionDate }}</el-descriptions-item>
        <el-descriptions-item label="严重程度">
          <el-tag v-if="detailData.severity" :type="getSeverityTagType(detailData.severity)" size="small" effect="dark">
            {{ getSeverityText(detailData.severity) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="客户名称">{{ detailData.customerName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ detailData.customerPhone }}</el-descriptions-item>
        <el-descriptions-item label="服务项目" :span="2">{{ detailData.serviceItem || '-' }}</el-descriptions-item>
        <el-descriptions-item label="关联订单" :span="2">{{ detailData.orderNo || '-' }}</el-descriptions-item>
        <el-descriptions-item label="反应描述" :span="2">{{ detailData.reaction || '-' }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, View } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { getServiceReactions, createServiceReaction, getCustomerOptions } from '@/api/customer-profile'
import type {
  ServiceReaction,
  ServiceReactionCreate,
  CustomerOption,
  SeverityLevel
} from '@/api/customer-profile/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerName: '',
  dateRange: [] as string[],
  severity: undefined as SeverityLevel | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<ServiceReaction[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 严重程度文本
const getSeverityText = (severity: SeverityLevel): string => {
  const map: Record<number, string> = { 1: '轻微', 2: '中等', 3: '严重' }
  return map[severity] || '未知'
}

// 严重程度标签颜色（轻微-绿色、中等-橙色、严重-红色）
const getSeverityTagType = (severity: SeverityLevel): 'success' | 'warning' | 'danger' => {
  const map: Record<number, 'success' | 'warning' | 'danger'> = {
    1: 'success',
    2: 'warning',
    3: 'danger'
  }
  return map[severity] || 'success'
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getServiceReactions({
      customerName: searchForm.customerName || undefined,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
      severity: searchForm.severity,
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
  searchForm.customerName = ''
  searchForm.dateRange = []
  searchForm.severity = undefined
  handleSearch()
}

// ==================== 新增弹窗 ====================

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const customerOptions = ref<CustomerOption[]>([])

const reactionForm = reactive<ServiceReactionCreate>({
  customerId: 0,
  serviceItem: '',
  reactionDate: new Date().toISOString().substring(0, 10),
  reaction: '',
  severity: 1,
  remark: ''
})

const formRules: FormRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  reactionDate: [{ required: true, message: '请选择反应日期', trigger: 'change' }]
}

// 新增
const handleAdd = async () => {
  if (customerOptions.value.length === 0) {
    customerOptions.value = await getCustomerOptions()
  }
  dialogVisible.value = true
}

// 弹窗关闭后重置
const handleDialogClosed = () => {
  formRef.value?.resetFields()
  reactionForm.customerId = 0
  reactionForm.serviceItem = ''
  reactionForm.reactionDate = new Date().toISOString().substring(0, 10)
  reactionForm.reaction = ''
  reactionForm.severity = 1
  reactionForm.remark = ''
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      await createServiceReaction({
        customerId: reactionForm.customerId,
        serviceItem: reactionForm.serviceItem || undefined,
        reactionDate: reactionForm.reactionDate,
        reaction: reactionForm.reaction || undefined,
        severity: reactionForm.severity,
        remark: reactionForm.remark || undefined
      })
      ElMessage.success('记录添加成功')
      dialogVisible.value = false
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '保存失败')
    } finally {
      submitLoading.value = false
    }
  })
}

// ==================== 详情弹窗 ====================

const detailVisible = ref(false)
const detailData = ref<ServiceReaction | null>(null)

const handleView = (row: ServiceReaction) => {
  detailData.value = row
  detailVisible.value = true
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
.service-reaction {
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

.text-muted {
  color: var(--text-tertiary);
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
