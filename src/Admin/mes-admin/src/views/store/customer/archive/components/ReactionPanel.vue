<template>
  <div class="reaction-panel">
    <!-- 搜索区域 -->
    <div class="search-form">
      <el-form :inline="true" :model="searchForm" class="search-form-inline">
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

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新增记录
        </el-button>
      </div>
      <div class="toolbar-right">
        <el-button circle @click="loadData">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <!-- 表格区域 -->
    <el-table v-loading="tableLoading" :data="tableData" style="width: 100%">
      <el-table-column prop="reactionDate" label="反应日期" width="120" />
      <el-table-column label="服务项目" min-width="140" show-overflow-tooltip>
        <template #default="{ row }">
          {{ row.productName || row.serviceItem || '-' }}
        </template>
      </el-table-column>
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

    <!-- 新增弹窗 -->
    <el-dialog v-model="dialogVisible" title="新增服务反应记录" width="560px" @closed="handleDialogClosed">
      <el-form ref="formRef" :model="reactionForm" :rules="formRules" label-width="100px">
        <el-form-item label="客户">
          <span>{{ props.customerName }}</span>
        </el-form-item>

        <el-form-item label="关联订单">
          <el-select
            v-model="reactionForm.orderId"
            placeholder="选填，可关联服务/疗程卡核销订单"
            filterable
            clearable
            :loading="orderOptionsLoading"
            style="width: 100%"
          >
            <el-option
              v-for="order in orderOptions"
              :key="order.id"
              :label="`${order.orderNo}（${order.orderType === 2 ? '服务' : '疗程卡核销'} · ${order.orderTime ? order.orderTime.substring(0, 10) : ''}）`"
              :value="order.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="服务项目" prop="productId">
          <el-select
            v-model="reactionForm.productId"
            placeholder="请选择服务项目"
            filterable
            clearable
            style="width: 100%"
          >
            <el-option v-for="p in serviceProductOptions" :key="p.id" :label="p.name" :value="p.id" />
          </el-select>
        </el-form-item>

        <el-form-item label="反应日期" prop="reactionDate">
          <el-date-picker
            v-model="reactionForm.reactionDate"
            type="date"
            placeholder="请选择反应日期"
            value-format="YYYY-MM-DD"
            :disabled-date="disableFutureDate"
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
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">保存</el-button>
      </template>
    </el-dialog>

    <!-- 详情弹窗 -->
    <el-dialog v-model="detailVisible" title="反应记录详情" width="560px">
      <el-descriptions v-if="detailData" :column="2" border>
        <el-descriptions-item label="反应日期">{{ detailData.reactionDate }}</el-descriptions-item>
        <el-descriptions-item label="严重程度">
          <el-tag
            v-if="detailData.severity"
            :type="getSeverityTagType(detailData.severity)"
            size="small"
            effect="dark"
          >
            {{ getSeverityText(detailData.severity) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="服务项目" :span="2">
          {{ detailData.productName || detailData.serviceItem || '-' }}
        </el-descriptions-item>
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
import { ref, reactive, watch, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, View } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { getServiceReactions, createServiceReaction } from '@/api/customer-profile'
import { getOrders } from '@/api/order'
import { getProducts } from '@/api/product'
import type { ServiceReaction, ServiceReactionCreate, SeverityLevel } from '@/api/customer-profile/types'
import type { Order } from '@/api/order/types'
import type { Product } from '@/api/product/types'

const props = defineProps<{
  /** 当前客户ID，由服务档案容器页提供 */
  customerId: number
  /** 当前客户姓名，仅用于弹窗内只读展示 */
  customerName: string
}>()

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
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
      customerId: props.customerId,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
      severity: searchForm.severity,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
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
  searchForm.severity = undefined
  handleSearch()
}

// ==================== 新增弹窗 ====================

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const orderOptions = ref<Order[]>([])
const orderOptionsLoading = ref(false)
const serviceProductOptions = ref<Product[]>([])

const reactionForm = reactive<Omit<ServiceReactionCreate, 'customerId'>>({
  orderId: undefined,
  productId: undefined,
  reactionDate: new Date().toISOString().substring(0, 10),
  reaction: '',
  severity: 1,
  remark: ''
})

const formRules: FormRules = {
  productId: [{ required: true, message: '请选择服务项目', trigger: 'change' }],
  reactionDate: [{ required: true, message: '请选择反应日期', trigger: 'change' }]
}

// 反应记录只能登记已发生的事实，故禁止选择当天之后的日期
const disableFutureDate = (date: Date) => date.getTime() > Date.now()

// 加载客户对应的服务/疗程卡核销订单
const loadOrderOptions = async (customerId: number | undefined) => {
  if (!customerId) {
    orderOptions.value = []
    return
  }
  orderOptionsLoading.value = true
  try {
    const res = await getOrders({ customerId, pageIndex: 1, pageSize: 9999 })
    // 前端过滤仅显示 orderType=2(服务) 或 3(疗程卡核销)
    orderOptions.value = res.list.filter(o => o.orderType === 2 || o.orderType === 3)
  } catch {
    ElMessage.error('加载订单列表失败')
    orderOptions.value = []
  } finally {
    orderOptionsLoading.value = false
  }
}

// 加载服务项目列表（仅商品类型为"服务项目"的上架商品）
const loadServiceProducts = async () => {
  try {
    const res = await getProducts({ type: 2, status: 1, pageIndex: 1, pageSize: 200 })
    serviceProductOptions.value = res.list
  } catch {
    serviceProductOptions.value = []
  }
}

// 客户切换时重载列表与订单选项，并归位分页
watch(
  () => props.customerId,
  id => {
    pagination.pageIndex = 1
    loadData()
    reactionForm.orderId = undefined
    loadOrderOptions(id)
  }
)

const handleAdd = async () => {
  if (serviceProductOptions.value.length === 0) {
    await loadServiceProducts()
  }
  dialogVisible.value = true
}

// 弹窗关闭后重置（订单选项由客户决定，不随弹窗清空）
const handleDialogClosed = () => {
  formRef.value?.resetFields()
  reactionForm.orderId = undefined
  reactionForm.productId = undefined
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
        customerId: props.customerId,
        orderId: reactionForm.orderId,
        productId: reactionForm.productId,
        reactionDate: reactionForm.reactionDate,
        reaction: reactionForm.reaction || undefined,
        severity: reactionForm.severity,
        remark: reactionForm.remark || undefined
      })
      ElMessage.success('记录添加成功')
      dialogVisible.value = false
      loadData()
    } catch (error) {
      ElMessage.error((error as Error).message || '保存失败')
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
  // 关联订单下拉原由"选择客户"触发加载，客户恒定后改为挂载时加载
  loadOrderOptions(props.customerId)
})
</script>

<style scoped>
.reaction-panel {
  width: 100%;
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
  padding-bottom: 4px;
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

.text-muted {
  color: var(--text-tertiary);
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 0 4px;
  border-top: 1px solid var(--border-primary);
}
</style>
