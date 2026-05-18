<template>
  <div class="audit-log-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="操作人">
            <el-input
              v-model="searchForm.userName"
              placeholder="请输入操作人姓名"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="操作类型">
            <el-select v-model="searchForm.operationType" placeholder="请选择操作类型" clearable style="width: 140px">
              <el-option label="全部" value="" />
              <el-option label="登录" value="Login" />
              <el-option label="登出" value="Logout" />
              <el-option label="新增" value="Create" />
              <el-option label="修改" value="Update" />
              <el-option label="删除" value="Delete" />
            </el-select>
          </el-form-item>
          <el-form-item label="操作时间">
            <el-date-picker
              v-model="searchForm.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 300px"
            />
          </el-form-item>
          <el-form-item label="响应状态">
            <el-select v-model="searchForm.responseStatus" placeholder="请选择" clearable style="width: 100px">
              <el-option label="成功" value="success" />
              <el-option label="失败" value="error" />
            </el-select>
          </el-form-item>
          <el-form-item label="租户" v-if="isSuperAdmin">
            <el-select v-model="searchForm.tenantId" placeholder="请选择租户" clearable filterable style="width: 150px">
              <el-option
                v-for="tenant in tenantList"
                :key="tenant.id"
                :label="tenant.name"
                :value="tenant.id"
              />
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
        <el-button
          @click="handleExport"
          v-if="hasPermission('system:auditLog:export')"
        >
          <el-icon><Download /></el-icon>
          导出日志
        </el-button>
        <el-button
          type="danger"
          @click="handleClearHistory"
          v-if="hasPermission('system:auditLog:clearHistory')"
        >
          <el-icon><Delete /></el-icon>
          清空历史
        </el-button>
      </div>
      <div class="toolbar-right">
        <div class="log-summary">
          <span class="summary-item">
            <span class="summary-label">总记录：</span>
            <span class="summary-value">{{ totalLogs }}</span>
          </span>
          <span class="summary-item">
            <span class="summary-label">今日：</span>
            <span class="summary-value">{{ todayLogs }}</span>
          </span>
        </div>
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
        <el-table-column prop="tenantId" label="租户" width="100" v-if="isSuperAdmin">
          <template #default="{ row }">
            <span>{{ getTenantName(row.tenantId) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="userName" label="操作人" width="100" />
        <el-table-column prop="operationType" label="操作类型" width="90">
          <template #default="{ row }">
            <el-tag :type="getOperationTypeColor(row.operationType)" size="small" effect="dark">
              {{ getOperationTypeLabel(row.operationType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="operationContent" label="操作内容" min-width="200" show-overflow-tooltip />
        <el-table-column prop="requestIp" label="请求IP" width="140">
          <template #default="{ row }">
            <span class="data-highlight">{{ row.requestIp }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="responseStatus" label="响应状态" width="100">
          <template #default="{ row }">
            <div class="status-cell">
              <span class="status-indicator" :class="getStatusClass(row.responseStatus)"></span>
              <span>{{ row.responseStatus }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="duration" label="耗时(ms)" width="100">
          <template #default="{ row }">
            <span class="data-highlight">{{ row.duration }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="操作时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleViewDetail(row)">
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

    <!-- 日志详情弹窗 -->
    <el-dialog
      v-model="detailVisible"
      title="审计日志详情"
      width="900px"
      :close-on-click-modal="false"
    >
      <div class="log-detail" v-if="currentLog">
        <!-- 基本信息 -->
        <div class="detail-section">
          <h3 class="section-title">
            <span class="title-text">基本信息</span>
            <span class="title-line"></span>
          </h3>
          <div class="detail-grid">
            <div class="detail-item">
              <span class="item-label">日志ID</span>
              <span class="item-value data-highlight">{{ currentLog.id }}</span>
            </div>
            <div class="detail-item">
              <span class="item-label">操作人</span>
              <span class="item-value">{{ currentLog.userName }} / {{ currentLog.realName }}</span>
            </div>
            <div class="detail-item">
              <span class="item-label">操作类型</span>
              <span class="item-value">
                <el-tag :type="getOperationTypeColor(currentLog.operationType)" size="small" effect="dark">
                  {{ getOperationTypeLabel(currentLog.operationType) }}
                </el-tag>
              </span>
            </div>
            <div class="detail-item">
              <span class="item-label">操作时间</span>
              <span class="item-value">{{ formatFullDate(currentLog.createdTime) }}</span>
            </div>
            <div class="detail-item">
              <span class="item-label">请求IP</span>
              <span class="item-value data-highlight">{{ currentLog.requestIp }}</span>
            </div>
            <div class="detail-item">
              <span class="item-label">请求方式</span>
              <span class="item-value code-tag">{{ currentLog.requestMethod }}</span>
            </div>
            <div class="detail-item">
              <span class="item-label">响应状态</span>
              <span class="item-value">
                <span class="status-indicator" :class="getStatusClass(currentLog.responseStatus)"></span>
                {{ currentLog.responseStatus }}
              </span>
            </div>
            <div class="detail-item">
              <span class="item-label">响应耗时</span>
              <span class="item-value data-highlight">{{ currentLog.duration }} ms</span>
            </div>
          </div>
        </div>

        <!-- 请求信息 -->
        <div class="detail-section">
          <h3 class="section-title">
            <span class="title-text">请求信息</span>
            <span class="title-line"></span>
          </h3>
          <div class="code-section">
            <div class="code-label">请求路径</div>
            <div class="code-content"><code>{{ currentLog.requestPath }}</code></div>
          </div>
          <div class="code-section">
            <div class="code-label">User Agent</div>
            <div class="code-content small-text"><code>{{ currentLog.userAgent || '-' }}</code></div>
          </div>
        </div>

        <!-- 变更内容 -->
        <div class="detail-section">
          <h3 class="section-title">
            <span class="title-text">变更内容</span>
            <span class="title-line"></span>
          </h3>
          <div class="code-section">
            <div class="code-label">实体变更</div>
            <div class="code-content">
              <pre><code>{{ formatJson(currentLog.entityChanges) }}</code></pre>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
        <el-button type="primary" @click="copyLog">
          <el-icon><DocumentCopy /></el-icon>
          复制日志
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Download, Delete, View, DocumentCopy } from '@element-plus/icons-vue'
import type { AuditLog, Tenant } from '@/api/system/types'
import { getAuditLogs, clearExpiredAuditLogs, getTenants } from '@/api/system'
import { useUserStore } from '@/stores/user'
import { useSystemConfigStore } from '@/stores/systemConfig'

const userStore = useUserStore()
const systemConfigStore = useSystemConfigStore()

// 判断是否有指定权限
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 判断是否为超级管理员
const isSuperAdmin = computed(() => userStore.isSuperAdmin)
// 获取当前登录用户的租户ID
const currentTenantId = computed(() => userStore.currentTenantId)

// 搜索表单
const searchForm = reactive({
  userName: '',
  operationType: '',
  dateRange: [] as Date[],
  responseStatus: '',
  tenantId: undefined as number | string | undefined
})

// 租户列表
const tenantList = ref<Tenant[]>([])

// 加载租户列表
const loadTenants = async () => {
  if (!isSuperAdmin.value) return
  try {
    const res = await getTenants({ pageIndex: 1, pageSize: 100 })
    tenantList.value = res.list
    // 确保 tenantList 加载完成后再设置 searchForm.tenantId
    await nextTick()
    if (searchForm.tenantId === undefined) {
      searchForm.tenantId = '1'
    }
  } catch (error) {
    console.error('加载租户失败', error)
  }
}

// 获取租户名称
const getTenantName = (tenantId: number | string | undefined) => {
  if (!tenantId) return '-'
  const tenant = tenantList.value.find(t => t.id === tenantId || String(t.id) === String(tenantId))
  return tenant?.name || String(tenantId)
}

// 表格数据
const tableLoading = ref(false)
const tableData = ref<AuditLog[]>([])
const totalLogs = ref(0)
const todayLogs = ref(0)

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 详情
const detailVisible = ref(false)
const currentLog = ref<AuditLog | null>(null)

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    // 超级管理员：使用选择的租户（非空则用选择的，否则默认平台租户）
    // 非超级管理员：使用当前登录用户的租户
    const effectiveTenantId = isSuperAdmin.value
      ? (searchForm.tenantId || 1)
      : currentTenantId.value

    const res = await getAuditLogs({
      userName: searchForm.userName || undefined,
      operationType: searchForm.operationType || undefined,
      startDate: searchForm.dateRange?.[0] ? `${searchForm.dateRange[0]}T00:00:00` : undefined,
      endDate: searchForm.dateRange?.[1] ? `${searchForm.dateRange[1]}T23:59:59` : undefined,
      responseStatus: searchForm.responseStatus || undefined,
      tenantId: effectiveTenantId,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list || []
    pagination.total = res.total || 0
    totalLogs.value = res.total || 0
    // 今日日志数计算
    const today = new Date().toISOString().split('T')[0]
    todayLogs.value = tableData.value.filter(log => {
      const logDate = new Date(log.createdTime).toISOString().split('T')[0]
      return logDate === today
    }).length
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
  searchForm.userName = ''
  searchForm.operationType = ''
  searchForm.dateRange = []
  searchForm.responseStatus = ''
  // 重置时根据用户角色设置默认租户
  searchForm.tenantId = isSuperAdmin.value ? '1' : currentTenantId.value
  handleSearch()
}

// 导出日志
const handleExport = async () => {
  try {
    await ElMessageBox.confirm('确定要导出当前的审计日志吗？', '提示', {
      type: 'info'
    })
    // 使用当前筛选条件导出数据
    const effectiveTenantId = isSuperAdmin.value
      ? (searchForm.tenantId || 1)
      : currentTenantId.value
    const res = await getAuditLogs({
      userName: searchForm.userName || undefined,
      operationType: searchForm.operationType || undefined,
      startDate: searchForm.dateRange?.[0] ? `${searchForm.dateRange[0]}T00:00:00` : undefined,
      endDate: searchForm.dateRange?.[1] ? `${searchForm.dateRange[1]}T23:59:59` : undefined,
      responseStatus: searchForm.responseStatus || undefined,
      tenantId: effectiveTenantId,
      pageIndex: 1,
      pageSize: 10000
    })
    const logs = res.list || []

    // 生成 CSV 内容
    const headers = ['操作人', '操作类型', '操作内容', '请求IP', '响应状态', '耗时(ms)', '操作时间']
    const rows = logs.map((log: AuditLog) => [
      log.userName || '',
      log.operationType || '',
      log.operationContent || '',
      log.requestIp || '',
      log.responseStatus || '',
      log.duration || '',
      log.createdTime || ''
    ])

    const csvContent = [headers, ...rows]
      .map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(','))
      .join('\n')

    // 生成文件名：超级管理员显示租户名，非超级管理员不显示
    let fileName = '审计日志'
    if (isSuperAdmin.value) {
      const tenant = tenantList.value.find(t => t.id === effectiveTenantId || String(t.id) === String(effectiveTenantId))
      const tenantName = tenant?.name || `租户${effectiveTenantId}`
      fileName += `_${tenantName}`
    }
    fileName += `_${new Date().toISOString().split('T')[0]}`

    // 下载文件
    const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `${fileName}.csv`
    link.click()
    URL.revokeObjectURL(link.href)

    ElMessage.success('导出成功')
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('导出失败')
    }
  }
}

// 清空历史
const handleClearHistory = async () => {
  try {
    await ElMessageBox.confirm('确定要清空历史日志吗？此操作不可恢复！', '警告', {
      type: 'warning',
      confirmButtonText: '确定清空',
      cancelButtonText: '取消'
    })
    const deletedCount = await clearExpiredAuditLogs()
    ElMessage.success(`已清理 ${deletedCount} 条历史日志`)
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('操作失败')
    }
  }
}

// 查看详情
const handleViewDetail = (row: AuditLog) => {
  currentLog.value = row
  detailVisible.value = true
}

// 复制日志
const copyLog = () => {
  if (!currentLog.value) return
  const text = JSON.stringify(currentLog.value, null, 2)
  navigator.clipboard.writeText(text).then(() => {
    ElMessage.success('日志已复制到剪贴板')
  })
}

// 获取操作类型标签
const getOperationTypeLabel = (type: string) => {
  const map: Record<string, string> = {
    Login: '登录',
    Logout: '登出',
    Create: '新增',
    Update: '修改',
    Delete: '删除'
  }
  return map[type] || type
}

// 获取操作类型颜色
const getOperationTypeColor = (type: string) => {
  const map: Record<string, any> = {
    Login: 'success',
    Logout: 'info',
    Create: 'success',
    Update: 'warning',
    Delete: 'danger'
  }
  return map[type] || 'info'
}

// 获取状态类
const getStatusClass = (status: number | string | null | undefined) => {
  if (!status) return 'warning'
  const numStatus = typeof status === 'number' ? status : parseInt(String(status))
  if (numStatus >= 200 && numStatus < 300) return 'success'
  if (numStatus >= 400 && numStatus < 500) return 'error'
  if (numStatus >= 500) return 'error'
  return 'warning'
}

// 格式化日期
const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

// 格式化完整日期
const formatFullDate = (dateStr: string) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('zh-CN')
}

// 格式化JSON
const formatJson = (data: any) => {
  if (!data) return '-'
  try {
    // 如果是已经序列化后的字符串，先解析再格式化
    const parsed = typeof data === 'string' ? JSON.parse(data) : data
    return JSON.stringify(parsed, null, 2)
  } catch {
    return data
  }
}

onMounted(async () => {
  // 确保系统配置已加载
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  // 应用默认分页大小
  pagination.pageSize = systemConfigStore.defaultPageSize
  // 初始化默认租户：超级管理员默认平台租户，非超级管理员默认当前用户租户
  searchForm.tenantId = isSuperAdmin.value ? '1' : currentTenantId.value
  loadData()
  loadTenants()
})
</script>

<style scoped>
.audit-log-management {
  width: 100%;
}

/* 卡片样式 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  position: relative;
  overflow: hidden;
}

.card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--primary), transparent);
  opacity: 0;
  transition: opacity 0.3s;
}

.card:hover::before {
  opacity: 1;
}

/* 表格样式 */
:deep(.el-table) {
  border-radius: var(--radius-lg);
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
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
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
  align-items: center;
  gap: 16px;
}

.log-summary {
  display: flex;
  gap: 20px;
  padding: 8px 16px;
  background: var(--bg-elevated);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
}

.summary-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.summary-label {
  font-size: 12px;
  color: var(--text-tertiary);
}

.summary-value {
  font-size: 16px;
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
  color: var(--primary);
}

/* 状态单元格 */
.status-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* 详情弹窗浅色主题 */
:deep(.el-dialog) {
  background: #ffffff !important;
  border: 1px solid #e5e7eb !important;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15) !important;
}

:deep(.el-dialog__header) {
  padding: 20px 24px;
  border-bottom: 1px solid #e5e7eb;
  background: #ffffff;
}

:deep(.el-dialog__title) {
  color: #1f2937;
  font-weight: 600;
}

:deep(.el-dialog__body) {
  padding: 0 !important;
  background: #ffffff;
}

:deep(.el-dialog__footer) {
  padding: 16px 24px;
  border-top: 1px solid #e5e7eb;
  background: #ffffff;
}

:deep(.el-dialog__headerbtn .el-dialog__close) {
  color: #9ca3af;
}

:deep(.el-dialog__headerbtn:hover .el-dialog__close) {
  color: #06b6d4;
}

/* 详情样式 */
.log-detail {
  padding: 24px;
  max-height: 600px;
  overflow-y: auto;
  background: #f9fafb;
  border-radius: 8px;
}

.detail-section {
  margin-bottom: 30px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 12px;
  margin: 0 0 20px;
  padding-bottom: 16px;
  border-bottom: 1px solid #e5e7eb;
}

.title-text {
  font-size: 15px;
  font-weight: 600;
  color: #1f2937;
  letter-spacing: 0.5px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-text::before {
  content: '';
  width: 4px;
  height: 16px;
  background: #06b6d4;
  border-radius: 2px;
}

.title-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, #e5e7eb 0%, transparent 100%);
  position: relative;
}

.title-line::after {
  content: '';
  position: absolute;
  right: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #06b6d4;
  opacity: 0.4;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px 30px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 12px 16px;
  background: #ffffff;
  border-radius: 6px;
  border: 1px solid #e5e7eb;
  transition: all 0.2s ease;
}

.detail-item:hover {
  border-color: #06b6d4;
  background: #f9fafb;
}

.item-label {
  font-size: 12px;
  color: #6b7280;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
}

.item-label::before {
  content: '';
  width: 3px;
  height: 3px;
  border-radius: 50%;
  background: #06b6d4;
  opacity: 0.6;
}

.item-value {
  font-size: 14px;
  color: #1f2937;
  font-weight: 500;
}

/* 代码展示区域 */
.code-section {
  margin-bottom: 16px;
}

.code-label {
  font-size: 12px;
  color: #6b7280;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
}

.code-label::before {
  content: '';
  width: 3px;
  height: 3px;
  border-radius: 50%;
  background: #06b6d4;
  opacity: 0.6;
}

.code-content {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 6px;
  padding: 16px;
  overflow-x: auto;
}

/* 浅色主题滚动条 */
.code-content::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.code-content::-webkit-scrollbar-track {
  background: #f3f4f6;
  border-radius: 4px;
}

.code-content::-webkit-scrollbar-thumb {
  background: #d1d5db;
  border-radius: 4px;
}

.code-content::-webkit-scrollbar-thumb:hover {
  background: #9ca3af;
}

.code-content code,
.code-content pre {
  font-family: 'JetBrains Mono', monospace;
  font-size: 13px;
  color: #1f2937;
  background: transparent;
  margin: 0;
}

.code-content small-text {
  font-size: 12px;
  color: #6b7280;
}

/* 标签样式 */
.code-tag {
  font-family: 'JetBrains Mono', monospace;
  background: rgba(6, 182, 212, 0.15);
  color: #06b6d4;
  padding: 2px 8px;
  border-radius: 4px;
}

/* 状态指示器 */
.status-indicator {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #d1d5db;
  position: relative;
}

.status-indicator::after {
  content: '';
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 16px;
  height: 16px;
  border-radius: 50%;
  background: inherit;
  opacity: 0.3;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    transform: translate(-50%, -50%) scale(0.8);
    opacity: 0.3;
  }
  70% {
    transform: translate(-50%, -50%) scale(1.5);
    opacity: 0;
  }
  100% {
    transform: translate(-50%, -50%) scale(0.8);
    opacity: 0;
  }
}

.status-indicator.success { background: #10b981; }
.status-indicator.warning { background: #f59e0b; }
.status-indicator.error { background: #ef4444; }
.status-indicator.info { background: #06b6d4; }

.status-indicator.success::after { background: #10b981; }
.status-indicator.warning::after { background: #f59e0b; }
.status-indicator.error::after { background: #ef4444; }
.status-indicator.info::after { background: #06b6d4; }

.status-indicator.success { box-shadow: 0 0 8px #10b981; }
.status-indicator.warning { box-shadow: 0 0 8px #f59e0b; }
.status-indicator.error { box-shadow: 0 0 8px #ef4444; }
.status-indicator.info { box-shadow: 0 0 8px #06b6d4; }

/* 数据高亮 */
.data-highlight {
  font-family: 'JetBrains Mono', monospace;
  color: #06b6d4;
  font-weight: 500;
}

/* 分页 - 科技风 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
