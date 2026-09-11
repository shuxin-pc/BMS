<template>
  <div class="technician-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="技师姓名/手机号">
            <el-input
              v-model="searchForm.keyword"
              placeholder="姓名或手机号"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="在岗" :value="1" />
              <el-option label="休息" :value="2" />
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
        <el-button type="primary" @click="handleAdd()" v-if="hasPermission('store:technician:profile:add')">
          <el-icon><Plus /></el-icon>
          新增技师
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0 || selectedRows.some(row => !canOperate(row))"
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
        <el-table-column label="可服务项目" min-width="200">
          <template #default="{ row }">
            <div v-if="technicianServicesMap[row.id] && technicianServicesMap[row.id].length" class="skill-tags">
              <el-tag
                v-for="name in technicianServicesMap[row.id]"
                :key="name"
                size="small"
                effect="plain"
                type="info"
                class="skill-tag"
              >
                {{ name }}
              </el-tag>
            </div>
            <span v-else class="text-muted">-</span>
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
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button
              link
              type="primary"
              size="small"
              :disabled="!canOperate(row)"
              @click="handleEdit(row)"
              v-if="hasPermission('store:technician:profile:edit')"
            >
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button
              link
              type="danger"
              size="small"
              :disabled="!canOperate(row)"
              @click="handleDelete(row)"
              v-if="hasPermission('store:technician:profile:delete')"
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
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="formData.phone" placeholder="请输入手机号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
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
            <el-form-item label="状态" prop="status">
              <el-radio-group v-model="formData.status">
                <el-radio :value="1">在岗</el-radio>
                <el-radio :value="2">休息</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <!-- 技师来源由后端根据当前租户强制赋值，前端无需录入 -->
        <el-form-item label="技能标签" prop="skillCategoryIds">
          <el-tree-select
            v-model="formData.skillCategoryIds"
            :data="skillCategoryTree"
            :props="{ label: 'name', children: 'children' }"
            node-key="id"
            multiple
            filterable
            clearable
            check-strictly
            placeholder="请选择技能标签"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="可服务项目">
          <template v-if="isEdit">
            <div class="skill-tags">
              <el-tag
                v-for="name in technicianServicesMap[formData.id] || []"
                :key="name"
                size="small"
                effect="plain"
                type="info"
                class="skill-tag"
              >
                {{ name }}
              </el-tag>
              <span v-if="!(technicianServicesMap[formData.id] && technicianServicesMap[formData.id].length)" class="text-muted">
                暂无可服务项目
              </span>
            </div>
          </template>
          <span v-else class="text-muted">保存后展示可服务项目</span>
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
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, User } from '@element-plus/icons-vue'
import {
  getTechnicians,
  createTechnician,
  updateTechnician,
  deleteTechnician,
  deleteTechnicians,
  getTechnicianServices
} from '@/api/staff'
import { getSkillCategoryTree } from '@/api/skill'
import type { SkillCategory } from '@/api/skill'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { Technician, TechnicianSource } from '@/api/staff/types'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  keyword: '',
  status: undefined as number | undefined
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

// 技师可服务项目名称缓存（id -> 名称列表，双向匹配展示用）
const technicianServicesMap = ref<Record<number, string[]>>({})

/**
 * 加载单个技师可服务项目名称（懒加载 + 缓存，避免重复请求）
 * @param technicianId 技师ID
 */
const loadTechnicianServices = async (technicianId: number) => {
  if (technicianServicesMap.value[technicianId]) return
  try {
    const list = await getTechnicianServices(technicianId)
    technicianServicesMap.value[technicianId] = list.map(s => s.name)
  } catch {
    technicianServicesMap.value[technicianId] = []
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTechnicians({
      keyword: searchForm.keyword || undefined,
      status: searchForm.status,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
    // 批量加载当前页技师的可服务项目（并行请求）
    await Promise.all(tableData.value.map(t => loadTechnicianServices(t.id)))
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
  searchForm.keyword = ''
  searchForm.status = undefined
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
  skillCategoryIds: [] as number[],
  status: 1 as number,
  source: 1 as TechnicianSource,
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '技师姓名不能为空', trigger: 'blur' },
    { max: 50, message: '技师姓名最多50个字符', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '手机号不能为空', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' }
  ],
  gender: [
    { required: true, message: '请选择性别', trigger: 'change' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
}

// 技能分类树形选项（保留层级结构，供 el-tree-select 展示）
const skillCategoryTree = ref<SkillCategory[]>([])

const loadSkillCategories = async () => {
  try {
    skillCategoryTree.value = await getSkillCategoryTree({})
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
  formData.skillCategoryIds = []
  formData.status = 1
  formData.source = 1
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
  formData.skillCategoryIds = row.skillCategoryIds ? [...row.skillCategoryIds] : []
  formData.status = row.status
  formData.source = row.source
  formData.remark = row.remark || ''
  // 编辑弹窗展示可服务项目（表格加载时已缓存则直接命中）
  loadTechnicianServices(row.id)
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
  } catch (error) {
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
  } catch (error) {
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
          status: formData.status,
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
      } catch (error) {
        ElMessage.error((error as Error).message || '操作失败')
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

/**
 * 判断当前用户是否可编辑/删除指定技师
 * 规则：技师归属租户与当前租户一致才可操作
 * - 商家门店（tenantId≠1）对平台技师（tenantId=1）只读，仅可操作本店自有技师
 * - 平台租户（tenantId=1）可维护本租户创建的平台技师
 */
const canOperate = (row: Technician): boolean => {
  const currentTenantId = userStore.currentTenantId
  if (!currentTenantId || row.tenantId == null) return false
  return String(row.tenantId) === String(currentTenantId)
}

// 状态文本与样式
const statusText = (status: number): string => {
  const map: Record<number, string> = { 1: '在岗', 2: '休息' }
  return map[status] || '未知'
}

const statusTagType = (status: number): 'success' | 'warning' | 'info' => {
  const map: Record<number, 'success' | 'warning' | 'info'> = {
    1: 'success',
    2: 'warning'
  }
  return map[status] || 'info'
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

/* 技能标签 */
.skill-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

/* 空状态占位 */
.text-muted {
  color: var(--text-tertiary);
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
