<template>
  <div class="technician-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="技师姓名">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入技师姓名"
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
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="在岗" :value="1" />
              <el-option label="休息" :value="2" />
              <el-option label="离职" :value="3" />
            </el-select>
          </el-form-item>
          <el-form-item label="职级">
            <el-select v-model="searchForm.level" placeholder="全部" clearable style="width: 120px">
              <el-option label="初级" :value="1" />
              <el-option label="中级" :value="2" />
              <el-option label="高级" :value="3" />
              <el-option label="总监" :value="4" />
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
        <el-button type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增技师
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
        >
          <el-icon><Delete /></el-icon>
          批量删除
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
        @selection-change="handleSelectionChange"
        style="width: 100%"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column label="技师信息" min-width="200">
          <template #default="{ row }">
            <div class="technician-info">
              <div class="technician-avatar">
                <el-icon><User /></el-icon>
              </div>
              <div class="technician-detail">
                <div class="technician-name">{{ row.name }}</div>
                <div class="technician-code">{{ row.jobNumber }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column label="性别" width="80" align="center">
          <template #default="{ row }">
            {{ row.gender === 1 ? '男' : '女' }}
          </template>
        </el-table-column>
        <el-table-column label="技能标签" min-width="220">
          <template #default="{ row }">
            <div class="skill-tags">
              <el-tag
                v-for="skill in row.skillCategoryNames"
                :key="skill"
                size="small"
                effect="plain"
                class="skill-tag"
              >
                {{ skill }}
              </el-tag>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="serviceItems" label="服务项目" min-width="200" show-overflow-tooltip />
        <el-table-column label="职级" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="levelTagType(row.level)" size="small" effect="dark">
              {{ levelText(row.level) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)" size="small" effect="dark">
              {{ statusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="来源" width="90" align="center">
          <template #default="{ row }">
            <el-tag
              :type="row.source === 1 ? 'primary' : 'warning'"
              size="small"
              effect="plain"
              :class="{ 'tag-platform': row.source === 2 }"
            >
              {{ row.source === 1 ? '自有' : '平台' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="hireDate" label="入职日期" width="120" />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button
              link
              type="primary"
              size="small"
              :disabled="row.source === 2"
              @click="handleEdit(row)"
            >
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button
              link
              type="danger"
              size="small"
              :disabled="row.source === 2"
              @click="handleDelete(row)"
            >
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
      :title="isEdit ? '编辑技师' : '新增技师'"
      width="680px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="技师姓名" prop="name">
              <el-input v-model="formData.name" placeholder="请输入技师姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="工号" prop="jobNumber">
              <el-input v-model="formData.jobNumber" placeholder="请输入工号" :disabled="isEdit" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="formData.phone" placeholder="请输入手机号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="性别" prop="gender">
              <el-radio-group v-model="formData.gender">
                <el-radio :value="1">男</el-radio>
                <el-radio :value="2">女</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="职级" prop="level">
              <el-select v-model="formData.level" placeholder="请选择职级" style="width: 100%">
                <el-option label="初级" :value="1" />
                <el-option label="中级" :value="2" />
                <el-option label="高级" :value="3" />
                <el-option label="总监" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态" prop="status">
              <el-radio-group v-model="formData.status">
                <el-radio :value="1">在岗</el-radio>
                <el-radio :value="2">休息</el-radio>
                <el-radio :value="3">离职</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <!-- 技师来源由后端根据当前租户强制赋值，前端无需录入 -->
        <el-form-item label="技能标签" prop="skillCategoryIds">
          <el-select
            v-model="formData.skillCategoryIds"
            multiple
            filterable
            clearable
            placeholder="请选择技能标签"
            style="width: 100%"
          >
            <el-option
              v-for="item in skillCategoryOptions"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="服务项目" prop="serviceItems">
          <el-input v-model="formData.serviceItems" placeholder="请输入服务项目，用顿号分隔" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="入职日期" prop="hireDate">
              <el-date-picker
                v-model="formData.hireDate"
                type="date"
                value-format="YYYY-MM-DD"
                placeholder="请选择入职日期"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
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
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, User } from '@element-plus/icons-vue'
import {
  getTechnicians,
  createTechnician,
  updateTechnician,
  deleteTechnician,
  deleteTechnicians
} from '@/api/staff'
import { getSkillCategoryTree } from '@/api/skill'
import type { SkillCategory } from '@/api/skill'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Technician, TechnicianSource } from '@/api/staff/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  phone: '',
  status: undefined as number | undefined,
  level: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Technician[]>([])
const selectedRows = ref<Technician[]>([])

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
    const res = await getTechnicians({
      name: searchForm.name || undefined,
      phone: searchForm.phone || undefined,
      status: searchForm.status,
      level: searchForm.level,
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
  searchForm.name = ''
  searchForm.phone = ''
  searchForm.status = undefined
  searchForm.level = undefined
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  phone: '',
  gender: 1 as number,
  jobNumber: '',
  skillCategoryIds: [] as number[],
  serviceItems: '',
  level: 1 as number,
  status: 1 as number,
  source: 1 as TechnicianSource,
  hireDate: '',
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '技师姓名不能为空', trigger: 'blur' },
    { max: 50, message: '技师姓名最多50个字符', trigger: 'blur' }
  ],
  jobNumber: [
    { required: true, message: '工号不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '工号长度为2-50个字符', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '手机号不能为空', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' }
  ],
  gender: [
    { required: true, message: '请选择性别', trigger: 'change' }
  ],
  level: [
    { required: true, message: '请选择职级', trigger: 'change' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ],
  hireDate: [
    { required: true, message: '请选择入职日期', trigger: 'change' }
  ],
  serviceItems: [
    { required: true, message: '服务项目不能为空', trigger: 'blur' }
  ]
}

// 技能分类选项
const skillCategoryOptions = ref<{ id: number; name: string }[]>([])

const loadSkillCategories = async () => {
  try {
    const tree = await getSkillCategoryTree({})
    const flatten = (nodes: SkillCategory[]): { id: number; name: string }[] => {
      const result: { id: number; name: string }[] = []
      nodes.forEach(node => {
        result.push({ id: node.id, name: node.name })
        if (node.children && node.children.length > 0) {
          result.push(...flatten(node.children))
        }
      })
      return result
    }
    skillCategoryOptions.value = flatten(tree)
  } catch {
    // 加载失败时静默处理
  }
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.phone = ''
  formData.gender = 1
  formData.jobNumber = ''
  formData.skillCategoryIds = []
  formData.serviceItems = ''
  formData.level = 1
  formData.status = 1
  formData.source = 1
  formData.hireDate = ''
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Technician) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.phone = row.phone
  formData.gender = row.gender
  formData.jobNumber = row.jobNumber || ''
  formData.skillCategoryIds = row.skillCategoryIds ? [...row.skillCategoryIds] : []
  formData.serviceItems = row.serviceItems || ''
  formData.level = row.level || 1
  formData.status = row.status
  formData.source = row.source
  formData.hireDate = row.hireDate || ''
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Technician) => {
  try {
    await ElMessageBox.confirm(`确定要删除技师 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteTechnician(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个技师吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteTechnicians(ids)
    ElMessage.success('批量删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
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
          name: formData.name,
          phone: formData.phone,
          gender: formData.gender,
          skillCategoryIds: formData.skillCategoryIds,
          jobNumber: formData.jobNumber,
          serviceItems: formData.serviceItems,
          level: formData.level,
          status: formData.status,
          hireDate: formData.hireDate,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateTechnician({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createTechnician(payload)
          ElMessage.success('创建成功')
        }
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

// 选择行
const handleSelectionChange = (rows: Technician[]) => {
  selectedRows.value = rows
}

// 状态文本与样式
const statusText = (status: number): string => {
  const map: Record<number, string> = { 1: '在岗', 2: '休息', 3: '离职' }
  return map[status] || '未知'
}

const statusTagType = (status: number): 'success' | 'warning' | 'info' => {
  const map: Record<number, 'success' | 'warning' | 'info'> = {
    1: 'success',
    2: 'warning',
    3: 'info'
  }
  return map[status] || 'info'
}

// 职级文本与样式
const levelText = (level: number): string => {
  const map: Record<number, string> = { 1: '初级', 2: '中级', 3: '高级', 4: '总监' }
  return map[level] || '未知'
}

const levelTagType = (level: number): 'info' | 'success' | 'warning' | 'danger' => {
  const map: Record<number, 'info' | 'success' | 'warning' | 'danger'> = {
    1: 'info',
    2: 'success',
    3: 'warning',
    4: 'danger'
  }
  return map[level] || 'info'
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  await loadSkillCategories()
  loadData()
})
</script>

<style scoped>
.technician-management {
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

/* 技师信息 */
.technician-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.technician-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: var(--bg-hover);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.technician-avatar .el-icon {
  color: var(--text-tertiary);
  font-size: 18px;
}

.technician-detail {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.technician-name {
  font-weight: 500;
  color: var(--text-primary);
}

.technician-code {
  font-size: 12px;
  color: var(--text-tertiary);
}

/* 技能标签 */
.skill-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.skill-tag {
  margin: 0;
}

/* 平台技师来源标签（紫色，区别于自有技师的蓝色） */
.tag-platform {
  background-color: #9c27b0 !important;
  border-color: #9c27b0 !important;
  color: #fff !important;
}

.skill-input-wrap {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.skill-input-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
