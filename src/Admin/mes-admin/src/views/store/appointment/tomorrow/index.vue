<template>
  <div class="tomorrow-reminder">
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
          <el-form-item label="手机号">
            <el-input
              v-model="searchForm.phone"
              placeholder="请输入手机号"
              clearable
              style="width: 150px"
            />
          </el-form-item>
          <el-form-item label="提醒状态">
            <el-select v-model="searchForm.remindStatus" placeholder="全部" clearable style="width: 120px">
              <el-option label="待提醒" :value="1" />
              <el-option label="已提醒" :value="2" />
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
        <el-button type="primary" :disabled="pendingRemindCount === 0" @click="handleBatchRemind">
          <el-icon><Promotion /></el-icon>
          批量发送提醒
        </el-button>
        <span class="toolbar-hint">
          共 {{ pagination.total }} 条预约，其中 {{ pendingRemindCount }} 条待提醒
        </span>
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
        <el-table-column type="selection" width="50" :selectable="canSelect" />
        <el-table-column prop="appointmentNo" label="预约编号" width="150" />
        <el-table-column prop="customerName" label="客户名称" width="100" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="serviceName" label="服务项目" width="120" show-overflow-tooltip />
        <el-table-column prop="technicianName" label="技师" width="90">
          <template #default="{ row }">
            {{ row.technicianName || '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="appointmentTime" label="预约时间" width="170" />
        <el-table-column label="提醒状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.remindStatus === 1 ? 'warning' : 'success'" size="small" effect="dark">
              {{ row.remindStatus === 1 ? '待提醒' : '已提醒' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="客户确认" width="110">
          <template #default="{ row }">
            <el-tag :type="getConfirmStatusType(row.customerConfirmStatus)" size="small">
              {{ getConfirmStatusText(row.customerConfirmStatus) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button
              v-if="row.remindStatus === 1"
              link
              type="primary"
              size="small"
              @click="handleSendRemind(row, 'sms')"
            >
              <el-icon><ChatDotRound /></el-icon>
              短信
            </el-button>
            <el-button
              v-if="row.remindStatus === 1"
              link
              type="success"
              size="small"
              @click="handleSendRemind(row, 'wechat')"
            >
              <el-icon><Message /></el-icon>
              微信
            </el-button>
            <el-button
              v-if="row.customerConfirmStatus !== 2"
              link
              type="warning"
              size="small"
              @click="handleConfirm(row)"
            >
              <el-icon><Check /></el-icon>
              确认
            </el-button>
            <span v-if="row.remindStatus === 2 && row.customerConfirmStatus === 2" class="text-tertiary">已完成</span>
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  Search,
  Refresh,
  Promotion,
  ChatDotRound,
  Message,
  Check
} from '@element-plus/icons-vue'
import {
  getTomorrowReminders,
  sendReminder,
  confirmTomorrowAppointment
} from '@/api/appointment'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { TomorrowReminder } from '@/api/appointment/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerName: '',
  phone: '',
  remindStatus: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<TomorrowReminder[]>([])
const selectedRows = ref<TomorrowReminder[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 待提醒数量
const pendingRemindCount = computed(() => {
  return tableData.value.filter(r => r.remindStatus === 1).length
})

// 客户确认状态文本
const getConfirmStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '待确认', 2: '已确认', 3: '需改期' }
  return map[status] || '未知'
}

// 客户确认状态标签类型
const getConfirmStatusType = (status: number): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: 'warning',
    2: 'success',
    3: 'danger'
  }
  return map[status] || 'info'
}

// 是否可选（只有待提醒的可选）
const canSelect = (row: TomorrowReminder) => row.remindStatus === 1

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTomorrowReminders({
      customerName: searchForm.customerName || undefined,
      phone: searchForm.phone || undefined,
      remindStatus: searchForm.remindStatus,
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
  searchForm.customerName = ''
  searchForm.phone = ''
  searchForm.remindStatus = undefined
  handleSearch()
}

// 选择行
const handleSelectionChange = (rows: TomorrowReminder[]) => {
  selectedRows.value = rows
}

// 发送提醒
const handleSendRemind = async (row: TomorrowReminder, channel: 'sms' | 'wechat') => {
  const channelName = channel === 'sms' ? '短信' : '微信'
  try {
    await ElMessageBox.confirm(`确定通过${channelName}发送提醒给 "${row.customerName}" 吗？`, '发送提醒', {
      type: 'info',
      confirmButtonText: '发送',
      cancelButtonText: '取消'
    })
    await sendReminder(row.id, channel)
    ElMessage.success(`${channelName}提醒已发送`)
    loadData()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('操作失败')
  }
}

// 批量发送提醒
const handleBatchRemind = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要批量发送 ${selectedRows.value.length} 条提醒吗？默认通过短信发送。`, '批量提醒', {
      type: 'info',
      confirmButtonText: '批量发送',
      cancelButtonText: '取消'
    })
    for (const row of selectedRows.value) {
      await sendReminder(row.id, 'sms')
    }
    ElMessage.success('批量提醒已发送')
    loadData()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('操作失败')
  }
}

// 确认预约
const handleConfirm = async (row: TomorrowReminder) => {
  try {
    await ElMessageBox.confirm(`确认客户 "${row.customerName}" 的预约吗？`, '确认预约', {
      type: 'success',
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
    await confirmTomorrowAppointment(row.id)
    ElMessage.success('已确认预约')
    loadData()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('操作失败')
  }
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
.tomorrow-reminder {
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

.toolbar-right {
  display: flex;
  gap: 8px;
}

.toolbar-hint {
  font-size: 13px;
  color: var(--text-tertiary);
}

.text-tertiary {
  color: var(--text-tertiary);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
