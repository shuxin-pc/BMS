<template>
  <div class="maintenance-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="关联设备">
            <el-select
              v-model="searchForm.equipmentId"
              placeholder="全部设备"
              clearable
              filterable
              style="width: 180px"
            >
              <el-option
                v-for="item in equipmentOptions"
                :key="item.id"
                :label="item.name"
                :value="item.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="保养类型">
            <el-select v-model="searchForm.maintenanceType" placeholder="全部" clearable style="width: 130px">
              <el-option label="日常保养" :value="1" />
              <el-option label="定期保养" :value="2" />
              <el-option label="维修" :value="3" />
            </el-select>
          </el-form-item>
          <el-form-item label="保养日期">
            <el-date-picker
              v-model="searchForm.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              format="YYYY-MM-DD"
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
        <el-button type="primary" @click="handleAdd" v-if="hasPermission('store:equipment:maintenance:add')">
          <el-icon><Plus /></el-icon>
          新增保养记录
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
      <el-table v-loading="tableLoading" :data="tableData" style="width: 100%">
        <el-table-column prop="equipmentName" label="关联设备" min-width="160" show-overflow-tooltip />
        <el-table-column prop="maintenanceType" label="保养类型" width="110">
          <template #default="{ row }">
            <el-tag :type="getTypeTagType(row.maintenanceType)" size="small" effect="plain">
              {{ getTypeText(row.maintenanceType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="cost" label="保养费用" width="120" align="right">
          <template #default="{ row }">
            {{ row.cost != null ? `¥${Number(row.cost).toLocaleString()}` : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="operator" label="操作人" width="120">
          <template #default="{ row }">{{ row.operator || '-' }}</template>
        </el-table-column>
        <el-table-column prop="result" label="保养结果" min-width="220" show-overflow-tooltip>
          <template #default="{ row }">{{ row.result || '-' }}</template>
        </el-table-column>
        <el-table-column prop="maintenanceDate" label="保养日期" width="120">
          <template #default="{ row }">{{ formatDate(row.maintenanceDate) }}</template>
        </el-table-column>
        <el-table-column prop="nextMaintenanceDate" label="下次保养日期" width="130">
          <template #default="{ row }">
            <span :class="{ 'maintenance-warn': isMaintenanceNear(row.nextMaintenanceDate) }">
              {{ formatDate(row.nextMaintenanceDate) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">{{ row.remark || '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)" v-if="hasPermission('store:equipment:maintenance:edit')">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)" v-if="hasPermission('store:equipment:maintenance:delete')">
              <el-icon><Delete /></el-icon>
              删除
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

    <!-- 新增/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑保养记录' : '新增保养记录'"
      width="640px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="110px"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="关联设备" prop="equipmentId">
              <el-select
                v-model="formData.equipmentId"
                placeholder="请选择设备"
                filterable
                style="width: 100%"
              >
                <el-option
                  v-for="item in equipmentOptions"
                  :key="item.id"
                  :label="`${item.name}（${item.code}）`"
                  :value="item.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="保养类型" prop="maintenanceType">
              <el-select v-model="formData.maintenanceType" placeholder="请选择" style="width: 100%">
                <el-option label="日常保养" :value="1" />
                <el-option label="定期保养" :value="2" />
                <el-option label="维修" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="保养日期" prop="maintenanceDate">
              <el-date-picker
                v-model="formData.maintenanceDate"
                type="date"
                placeholder="选择保养日期"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="保养费用" prop="cost">
              <el-input-number
                v-model="formData.cost"
                :min="0"
                :precision="2"
                :step="100"
                placeholder="请输入保养费用"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="操作人" prop="operator">
              <el-input v-model="formData.operator" placeholder="请输入操作人" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="下次保养日期" prop="nextMaintenanceDate">
              <el-date-picker
                v-model="formData.nextMaintenanceDate"
                type="date"
                placeholder="选择下次保养日期"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="保养结果" prop="result">
          <el-input v-model="formData.result" type="textarea" :rows="2" placeholder="请输入保养结果" />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import { formatDate } from '@/utils/date'
import {
  getMaintenanceList,
  createMaintenance,
  updateMaintenance,
  deleteMaintenance,
  getAllEquipments
} from '@/api/equipment'
import type {
  EquipmentMaintenance,
  Equipment,
  MaintenanceType
} from '@/api/equipment/types'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  equipmentId: undefined as number | undefined,
  maintenanceType: undefined as MaintenanceType | undefined,
  dateRange: [] as string[]
})

// 设备下拉选项
const equipmentOptions = ref<Equipment[]>([])

// 表格数据
const tableLoading = ref(false)
const tableData = ref<EquipmentMaintenance[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  equipmentId: undefined as number | undefined,
  maintenanceType: 1 as MaintenanceType,
  maintenanceDate: '',
  cost: undefined as number | undefined,
  operator: '',
  result: '',
  nextMaintenanceDate: '',
  remark: ''
})

const formRules: FormRules = {
  equipmentId: [
    { required: true, message: '请选择关联设备', trigger: 'change' }
  ],
  maintenanceType: [
    { required: true, message: '请选择保养类型', trigger: 'change' }
  ],
  maintenanceDate: [
    { required: true, message: '请选择保养日期', trigger: 'change' }
  ]
}

/**
 * 获取保养类型文本
 */
const getTypeText = (type: MaintenanceType): string => {
  const map: Record<MaintenanceType, string> = {
    1: '日常保养',
    2: '定期保养',
    3: '维修'
  }
  return map[type] || '未知'
}

/**
 * 获取保养类型标签样式
 */
const getTypeTagType = (type: MaintenanceType): 'info' | 'success' | 'warning' => {
  const map: Record<MaintenanceType, 'info' | 'success' | 'warning'> = {
    1: 'info',
    2: 'success',
    3: 'warning'
  }
  return map[type] || 'info'
}


/**
 * 判断保养日期是否临近（7天内）
 */
const isMaintenanceNear = (dateStr?: string): boolean => {
  if (!dateStr) return false
  const target = new Date(dateStr)
  const now = new Date()
  const diff = target.getTime() - now.getTime()
  const days = diff / (1000 * 60 * 60 * 24)
  return days >= 0 && days <= 7
}

// 加载设备下拉数据
const loadEquipmentOptions = async () => {
  try {
    equipmentOptions.value = await getAllEquipments()
  } catch {
    // 选项加载失败不阻塞主流程
    equipmentOptions.value = []
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getMaintenanceList({
      equipmentId: searchForm.equipmentId,
      maintenanceType: searchForm.maintenanceType,
      startDate: searchForm.dateRange?.[0],
      endDate: searchForm.dateRange?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error((error as Error).message || '加载数据失败')
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
  searchForm.equipmentId = undefined
  searchForm.maintenanceType = undefined
  searchForm.dateRange = []
  handleSearch()
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.equipmentId = undefined
  formData.maintenanceType = 1
  formData.maintenanceDate = ''
  formData.cost = undefined
  formData.operator = ''
  formData.result = ''
  formData.nextMaintenanceDate = ''
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  // 默认保养日期为今天
  formData.maintenanceDate = formatDate(new Date())
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: EquipmentMaintenance) => {
  isEdit.value = true
  formData.id = row.id
  formData.equipmentId = row.equipmentId
  formData.maintenanceType = row.maintenanceType
  formData.maintenanceDate = row.maintenanceDate
  formData.cost = row.cost
  formData.operator = row.operator || ''
  formData.result = row.result || ''
  formData.nextMaintenanceDate = row.nextMaintenanceDate || ''
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: EquipmentMaintenance) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除该保养记录吗？（设备：${row.equipmentName}，日期：${formatDate(row.maintenanceDate)}）`,
      '警告',
      {
        type: 'warning',
        confirmButtonText: '确定删除',
        cancelButtonText: '取消'
      }
    )
    await deleteMaintenance(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '删除失败')
    }
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        const payload = {
          equipmentId: formData.equipmentId!,
          maintenanceType: formData.maintenanceType,
          maintenanceDate: formData.maintenanceDate,
          cost: formData.cost,
          operator: formData.operator || undefined,
          result: formData.result || undefined,
          nextMaintenanceDate: formData.nextMaintenanceDate || undefined,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateMaintenance({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createMaintenance(payload)
          ElMessage.success('创建成功')
        }
        dialogVisible.value = false
        loadData()
      } catch (error) {
        ElMessage.error((error as Error).message || '操作失败')
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
  await loadEquipmentOptions()
  loadData()
})
</script>

<style scoped>
.maintenance-management {
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

/* 保养日期临近提醒 */
.maintenance-warn {
  color: var(--warning, #e6a23c);
  font-weight: 500;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
