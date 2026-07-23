<template>
  <div class="sample-outbound">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="赠品名称">
            <el-input
              v-model="searchForm.sampleName"
              placeholder="请输入赠品名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="关联活动">
            <el-input
              v-model="searchForm.activityName"
              placeholder="请输入活动名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="时间范围">
            <el-date-picker
              v-model="dateRange"
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
    </div>

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增出库
        </el-button>
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
        <el-table-column prop="outboundNo" label="出库单号" width="160" />
        <el-table-column prop="sampleName" label="赠品名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="quantity" label="出库数量" width="100" align="center" />
        <el-table-column prop="activityName" label="关联活动" min-width="180" show-overflow-tooltip />
        <el-table-column prop="outboundTime" label="出库时间" width="170" />
        <el-table-column prop="operator" label="出库人" width="100" />
        <el-table-column prop="storeName" label="门店名称" width="120" />
        <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
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
    <el-dialog
      v-model="dialogVisible"
      title="新增出库"
      width="560px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="赠品" prop="sampleId">
          <el-select
            v-model="formData.sampleId"
            placeholder="请选择赠品"
            filterable
            style="width: 100%"
            @change="handleSampleChange"
          >
            <el-option
              v-for="item in sampleOptions"
              :key="item.id"
              :label="`${item.name}（${item.code}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="库存批次" prop="inventoryBatchId">
          <el-select
            v-model="formData.inventoryBatchId"
            placeholder="请选择库存批次"
            :loading="batchLoading"
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="item in batchOptions"
              :key="item.id"
              :label="`${item.batchNo}（过期：${item.expirationDate ? item.expirationDate.slice(0, 10) : '无'}，库存：${item.quantity}）`"
              :value="item.id"
              :disabled="item.quantity <= 0"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="出库数量" prop="quantity">
              <el-input-number
                v-model="formData.quantity"
                :min="1"
                :step="1"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="门店名称" prop="storeName">
              <el-input v-model="formData.storeName" placeholder="请输入门店名称" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="关联活动" prop="activityName">
          <el-input v-model="formData.activityName" placeholder="请输入关联活动名称" />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'
import {
  getSampleOutbounds,
  createSampleOutbound,
  getAllSamples
} from '@/api/sample'
import { getInventoryBatchList } from '@/api/inventory'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { SampleOutbound, Sample } from '@/api/sample/types'
import type { InventoryBatch } from '@/api/inventory/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  sampleName: '',
  activityName: ''
})

// 时间范围
const dateRange = ref<[string, string] | null>(null)

// 表格数据
const tableLoading = ref(false)
const tableData = ref<SampleOutbound[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 样品下拉选项
const sampleOptions = ref<Sample[]>([])

// 批次下拉选项
const batchOptions = ref<InventoryBatch[]>([])
const batchLoading = ref(false)

// 加载样品下拉
const loadSampleOptions = async () => {
  try {
    sampleOptions.value = await getAllSamples()
  } catch (error) {
    sampleOptions.value = []
  }
}

// 加载批次下拉（样品/赠品即 Product，id 即为 productId）
const loadBatchOptions = async (sampleId: number | undefined) => {
  batchOptions.value = []
  formData.inventoryBatchId = undefined
  if (!sampleId) return
  batchLoading.value = true
  try {
    const res = await getInventoryBatchList({
      productId: sampleId,
      status: 1, // 只查询在库批次
      pageIndex: 1,
      pageSize: 9999
    })
    batchOptions.value = res.list
    if (res.list.length === 0) {
      ElMessage.warning('该赠品暂无在库批次，请先入库')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '加载批次列表失败')
    batchOptions.value = []
  } finally {
    batchLoading.value = false
  }
}

// 样品选择变更时加载批次列表
const handleSampleChange = (sampleId: number | undefined) => {
  loadBatchOptions(sampleId)
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    // TODO: 后端 SampleOutboundQuery 不支持 sampleName/activityName/startDate/endDate 查询
    const res = await getSampleOutbounds({
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
  searchForm.sampleName = ''
  searchForm.activityName = ''
  dateRange.value = null
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  sampleId: undefined as number | undefined,
  inventoryBatchId: undefined as number | undefined,
  quantity: 1,
  activityName: '',
  storeName: '',
  remark: ''
})

const formRules: FormRules = {
  sampleId: [
    { required: true, message: '请选择赠品', trigger: 'change' }
  ],
  inventoryBatchId: [
    { required: true, message: '请选择库存批次', trigger: 'change' }
  ],
  quantity: [
    { required: true, message: '出库数量不能为空', trigger: 'blur' },
    { type: 'number', min: 1, message: '出库数量必须大于0', trigger: 'blur' }
  ],
  activityName: [
    { required: true, message: '关联活动不能为空', trigger: 'blur' },
    { max: 100, message: '活动名称最多100个字符', trigger: 'blur' }
  ],
  storeName: [
    { required: true, message: '门店名称不能为空', trigger: 'blur' },
    { max: 50, message: '门店名称最多50个字符', trigger: 'blur' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.sampleId = undefined
  formData.inventoryBatchId = undefined
  formData.quantity = 1
  formData.activityName = ''
  formData.storeName = ''
  formData.remark = ''
  batchOptions.value = []
}

// 新增
const handleAdd = () => {
  resetFormData()
  loadSampleOptions()
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        // TODO: 后端 SampleOutboundCreate 需要 productId/outTime，表单缺少部分字段
        await createSampleOutbound({
          productId: formData.sampleId!,
          inventoryBatchId: formData.inventoryBatchId!,
          quantity: formData.quantity,
          outTime: new Date().toISOString(),
          remark: formData.remark || undefined
        })
        ElMessage.success('出库成功')
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '操作失败')
      } finally {
        submitLoading.value = false
      }
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
.sample-outbound {
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
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
