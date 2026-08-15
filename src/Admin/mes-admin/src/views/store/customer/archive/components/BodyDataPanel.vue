<template>
  <div class="body-data-panel">
    <!-- 搜索区域 -->
    <div class="search-form">
      <el-form :inline="true" :model="searchForm" class="search-form-inline">
        <el-form-item label="记录日期">
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
      <el-table-column prop="recordDate" label="记录日期" width="120" />
      <el-table-column label="体重(kg)" width="100" align="right">
        <template #default="{ row }">
          <span v-if="row.weight != null" class="data-value">{{ row.weight.toFixed(1) }}</span>
          <span v-else class="text-muted">-</span>
        </template>
      </el-table-column>
      <el-table-column label="体脂率(%)" width="110" align="right">
        <template #default="{ row }">
          <span v-if="row.bodyFat != null" class="data-value" :class="{ 'data-warn': row.bodyFat > 28 }">
            {{ row.bodyFat.toFixed(1) }}
          </span>
          <span v-else class="text-muted">-</span>
        </template>
      </el-table-column>
      <el-table-column label="胸围(cm)" width="100" align="right">
        <template #default="{ row }">
          <span v-if="row.bust != null">{{ row.bust.toFixed(1) }}</span>
          <span v-else class="text-muted">-</span>
        </template>
      </el-table-column>
      <el-table-column label="腰围(cm)" width="100" align="right">
        <template #default="{ row }">
          <span v-if="row.waist != null">{{ row.waist.toFixed(1) }}</span>
          <span v-else class="text-muted">-</span>
        </template>
      </el-table-column>
      <el-table-column label="臀围(cm)" width="100" align="right">
        <template #default="{ row }">
          <span v-if="row.hip != null">{{ row.hip.toFixed(1) }}</span>
          <span v-else class="text-muted">-</span>
        </template>
      </el-table-column>
      <el-table-column prop="remark" label="备注" min-width="140" show-overflow-tooltip />
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
    <el-dialog v-model="dialogVisible" title="新增身体数据记录" width="560px" @closed="handleDialogClosed">
      <el-form ref="formRef" :model="bodyForm" :rules="formRules" label-width="100px">
        <el-form-item label="客户">
          <span>{{ props.customerName }}</span>
        </el-form-item>

        <el-form-item label="记录日期" prop="recordDate">
          <el-date-picker
            v-model="bodyForm.recordDate"
            type="date"
            placeholder="请选择记录日期"
            value-format="YYYY-MM-DD"
            :disabled-date="disableFutureDate"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item label="体重(kg)">
          <el-input-number
            v-model="bodyForm.weight"
            :min="20"
            :max="300"
            :precision="1"
            :step="0.1"
            style="width: 100%"
            placeholder="请输入体重"
          />
        </el-form-item>

        <el-form-item label="体脂率(%)">
          <el-input-number
            v-model="bodyForm.bodyFat"
            :min="3"
            :max="60"
            :precision="1"
            :step="0.1"
            style="width: 100%"
            placeholder="请输入体脂率"
          />
        </el-form-item>

        <el-form-item label="胸围(cm)">
          <el-input-number
            v-model="bodyForm.bust"
            :min="50"
            :max="150"
            :precision="1"
            :step="0.5"
            style="width: 100%"
            placeholder="请输入胸围"
          />
        </el-form-item>

        <el-form-item label="腰围(cm)">
          <el-input-number
            v-model="bodyForm.waist"
            :min="40"
            :max="150"
            :precision="1"
            :step="0.5"
            style="width: 100%"
            placeholder="请输入腰围"
          />
        </el-form-item>

        <el-form-item label="臀围(cm)">
          <el-input-number
            v-model="bodyForm.hip"
            :min="50"
            :max="150"
            :precision="1"
            :step="0.5"
            style="width: 100%"
            placeholder="请输入臀围"
          />
        </el-form-item>

        <el-form-item label="备注">
          <el-input
            v-model="bodyForm.remark"
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
    <el-dialog v-model="detailVisible" title="身体数据详情" width="520px">
      <el-descriptions v-if="detailData" :column="2" border>
        <el-descriptions-item label="记录日期">{{ detailData.recordDate }}</el-descriptions-item>
        <el-descriptions-item label="体重(kg)">{{ detailData.weight?.toFixed(1) ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="体脂率(%)">{{ detailData.bodyFat?.toFixed(1) ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="胸围(cm)">{{ detailData.bust?.toFixed(1) ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="腰围(cm)">{{ detailData.waist?.toFixed(1) ?? '-' }}</el-descriptions-item>
        <el-descriptions-item label="臀围(cm)">{{ detailData.hip?.toFixed(1) ?? '-' }}</el-descriptions-item>
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
import { getBodyDataRecords, createBodyDataRecord } from '@/api/customer-profile'
import type { BodyDataRecord, BodyDataCreate } from '@/api/customer-profile/types'

const props = defineProps<{
  /** 当前客户ID，由服务档案容器页提供 */
  customerId: number
  /** 当前客户姓名，仅用于弹窗内只读展示 */
  customerName: string
}>()

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<BodyDataRecord[]>([])

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
    const res = await getBodyDataRecords({
      customerId: props.customerId,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
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
  handleSearch()
}

// 客户切换时重载并归位分页；首屏由 onMounted 负责，故不加 immediate
watch(
  () => props.customerId,
  () => {
    pagination.pageIndex = 1
    loadData()
  }
)

// ==================== 新增弹窗 ====================

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const bodyForm = reactive<Omit<BodyDataCreate, 'customerId'>>({
  recordDate: new Date().toISOString().substring(0, 10),
  weight: undefined,
  bodyFat: undefined,
  bust: undefined,
  waist: undefined,
  hip: undefined,
  remark: ''
})

const formRules: FormRules = {
  recordDate: [{ required: true, message: '请选择记录日期', trigger: 'change' }]
}

// 身体数据只能录入已发生的测量结果，故禁止选择当天之后的日期
const disableFutureDate = (date: Date) => date.getTime() > Date.now()

const handleAdd = () => {
  dialogVisible.value = true
}

// 弹窗关闭后重置
const handleDialogClosed = () => {
  formRef.value?.resetFields()
  bodyForm.recordDate = new Date().toISOString().substring(0, 10)
  bodyForm.weight = undefined
  bodyForm.bodyFat = undefined
  bodyForm.bust = undefined
  bodyForm.waist = undefined
  bodyForm.hip = undefined
  bodyForm.remark = ''
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      await createBodyDataRecord({
        customerId: props.customerId,
        recordDate: bodyForm.recordDate,
        weight: bodyForm.weight,
        bodyFat: bodyForm.bodyFat,
        bust: bodyForm.bust,
        waist: bodyForm.waist,
        hip: bodyForm.hip,
        remark: bodyForm.remark || undefined
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
const detailData = ref<BodyDataRecord | null>(null)

const handleView = (row: BodyDataRecord) => {
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
.body-data-panel {
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

.data-value {
  color: var(--text-primary);
  font-weight: 600;
}

.data-warn {
  color: var(--warning);
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 0 4px;
  border-top: 1px solid var(--border-primary);
}
</style>
