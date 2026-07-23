<template>
  <el-dialog
    v-model="visible"
    width="1100px"
    :close-on-click-modal="false"
    destroy-on-close
    align-center
  >
    <div class="inbox-dialog">
      <!-- 搜索区域 -->
      <div class="search-bar">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="消息分类">
            <el-select v-model="searchForm.category" placeholder="请选择分类" clearable style="width: 140px">
              <el-option label="全部" value="" />
              <el-option label="系统消息" :value="MessageCategory.System" />
              <el-option label="业务通知" :value="MessageCategory.Business" />
              <el-option label="公告信息" :value="MessageCategory.Announcement" />
            </el-select>
          </el-form-item>
          <el-form-item label="阅读状态">
            <el-select v-model="searchForm.isRead" placeholder="请选择" clearable style="width: 120px">
              <el-option label="全部" value="" />
              <el-option label="未读" :value="false" />
              <el-option label="已读" :value="true" />
            </el-select>
          </el-form-item>
          <el-form-item label="接收时间">
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
          <el-button @click="handleBatchRead" :disabled="selectedIds.length === 0">
            <el-icon><Check /></el-icon>
            批量已读
          </el-button>
          <el-button type="danger" @click="handleBatchDelete" :disabled="selectedIds.length === 0">
            <el-icon><Delete /></el-icon>
            批量删除
          </el-button>
        </div>
        <div class="toolbar-right">
          <span class="unread-summary" v-if="messageStore.unreadCount > 0">
            <el-badge :value="messageStore.unreadCount" :max="99" type="danger">未读消息</el-badge>
          </span>
          <el-button circle @click="loadData">
            <el-icon><Refresh /></el-icon>
          </el-button>
        </div>
      </div>

      <!-- 表格区域 -->
      <el-table
        v-loading="tableLoading"
        :data="tableData"
        style="width: 100%"
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column prop="title" label="标题" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <div class="title-cell" :class="{ 'is-unread': !row.isRead }">
              <span class="unread-dot" v-if="!row.isRead"></span>
              <span class="title-text" @click="handleViewDetail(row)">{{ row.title }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="categoryName" label="分类" width="100">
          <template #default="{ row }">
            <el-tag :type="getCategoryTagType(row.category)" size="small" effect="plain">
              {{ row.categoryName }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="senderName" label="发送人" width="120" show-overflow-tooltip />
        <el-table-column prop="isRead" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isRead ? 'info' : 'danger'" size="small" effect="dark">
              {{ row.isRead ? '已读' : '未读' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="接收时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleViewDetail(row)">查看</el-button>
            <el-button link type="primary" v-if="!row.isRead" @click="handleMarkRead(row)">标记已读</el-button>
            <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pagination.pageIndex"
          v-model:page-size="pagination.pageSize"
          :total="pagination.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>

    <!-- 消息详情弹窗（嵌套弹窗，append-to-body 避免 z-index 被外层弹窗覆盖） -->
    <el-dialog
      v-model="detailVisible"
      width="600px"
      append-to-body
      :close-on-click-modal="false"
      align-center
    >
      <div v-if="currentMessage" class="message-detail">
        <h3 class="detail-title">{{ currentMessage.title }}</h3>
        <div class="detail-meta">
          <el-tag :type="getCategoryTagType(currentMessage.category)" size="small" effect="plain">
            {{ currentMessage.categoryName }}
          </el-tag>
          <span class="detail-meta-item">发送人：{{ currentMessage.senderName }}</span>
          <span class="detail-meta-item">时间：{{ formatDateTime(currentMessage.createdTime) }}</span>
        </div>
        <el-divider />
        <div class="detail-content">{{ currentMessage.content }}</div>
        <div class="detail-action" v-if="currentMessage.targetUrl">
          <el-button type="primary" @click="handleGotoTarget(currentMessage)">前往处理</el-button>
        </div>
      </div>
    </el-dialog>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Check, Delete } from '@element-plus/icons-vue'
import {
  getInbox,
  markAsRead,
  batchMarkAsRead,
  deleteMessage,
  batchDelete
} from '@/api/message'
import { useMessageStore } from '@/stores/message'
import { MessageCategory, type MessageInboxItem, type MessageInboxQuery } from '@/api/message/types'

const props = defineProps<{
  modelValue: boolean
}>()
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()

const router = useRouter()
const messageStore = useMessageStore()

// 弹窗显隐双向绑定
const visible = ref(props.modelValue)
watch(() => props.modelValue, (val) => {
  visible.value = val
  // 打开弹窗时加载数据
  if (val) {
    loadData()
  }
})
watch(visible, (val) => {
  emit('update:modelValue', val)
})

const tableLoading = ref(false)
const tableData = ref<MessageInboxItem[]>([])
const selectedIds = ref<number[]>([])
const detailVisible = ref(false)
const currentMessage = ref<MessageInboxItem | null>(null)

const searchForm = reactive({
  category: '' as number | '',
  isRead: '' as boolean | '',
  dateRange: [] as string[]
})

const pagination = reactive({
  pageIndex: 1,
  pageSize: 10,
  total: 0
})

/**
 * 构建查询参数
 */
function buildQuery(): MessageInboxQuery {
  const query: MessageInboxQuery = {
    pageIndex: pagination.pageIndex,
    pageSize: pagination.pageSize
  }
  if (searchForm.category !== '') {
    query.category = searchForm.category as number
  }
  if (searchForm.isRead !== '') {
    query.isRead = searchForm.isRead as boolean
  }
  if (searchForm.dateRange && searchForm.dateRange.length === 2) {
    query.startTime = `${searchForm.dateRange[0]} 00:00:00`
    query.endTime = `${searchForm.dateRange[1]} 23:59:59`
  }
  return query
}

/**
 * 加载收件箱数据
 */
async function loadData() {
  tableLoading.value = true
  try {
    const result = await getInbox(buildQuery())
    tableData.value = result.list || []
    pagination.total = result.total || 0
  } catch (error: any) {
    ElMessage.error(error.message || '加载失败')
  } finally {
    tableLoading.value = false
  }
}

/**
 * 搜索
 */
function handleSearch() {
  pagination.pageIndex = 1
  loadData()
}

/**
 * 重置
 */
function handleReset() {
  searchForm.category = ''
  searchForm.isRead = ''
  searchForm.dateRange = []
  pagination.pageIndex = 1
  loadData()
}

/**
 * 多选变化
 */
function handleSelectionChange(rows: MessageInboxItem[]) {
  selectedIds.value = rows.map(r => r.id)
}

/**
 * 查看详情
 */
async function handleViewDetail(row: MessageInboxItem) {
  currentMessage.value = row
  detailVisible.value = true

  // 未读消息自动标记已读
  if (!row.isRead) {
    try {
      await markAsRead(row.id)
      row.isRead = true
      messageStore.onMessageRead()
    } catch (error) {
      // 标记失败不影响查看
    }
  }
}

/**
 * 标记单条已读
 */
async function handleMarkRead(row: MessageInboxItem) {
  try {
    await markAsRead(row.id)
    row.isRead = true
    messageStore.onMessageRead()
    ElMessage.success('已标记为已读')
  } catch (error: any) {
    ElMessage.error(error.message || '操作失败')
  }
}

/**
 * 批量已读
 */
async function handleBatchRead() {
  if (selectedIds.value.length === 0) return
  try {
    await batchMarkAsRead(selectedIds.value)
    ElMessage.success('批量标记成功')
    messageStore.fetchUnreadCount()
    loadData()
  } catch (error: any) {
    ElMessage.error(error.message || '操作失败')
  }
}

/**
 * 删除单条
 */
async function handleDelete(row: MessageInboxItem) {
  try {
    await ElMessageBox.confirm('确定删除该消息吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await deleteMessage(row.id)
    ElMessage.success('删除成功')
    // 如果删除的是未读消息，更新未读数
    if (!row.isRead) {
      messageStore.onMessageRead()
    }
    loadData()
  } catch (error: any) {
    if (error !== 'cancel' && error?.message) {
      ElMessage.error(error.message)
    }
  }
}

/**
 * 批量删除
 */
async function handleBatchDelete() {
  if (selectedIds.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定删除选中的 ${selectedIds.value.length} 条消息吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await batchDelete(selectedIds.value)
    ElMessage.success('批量删除成功')
    messageStore.fetchUnreadCount()
    loadData()
  } catch (error: any) {
    if (error !== 'cancel' && error?.message) {
      ElMessage.error(error.message)
    }
  }
}

/**
 * 跳转到消息关联页面（关闭收件箱弹窗后跳转）
 */
function handleGotoTarget(msg: MessageInboxItem) {
  if (!msg.targetUrl) return
  detailVisible.value = false
  visible.value = false
  router.push(msg.targetUrl)
}

/**
 * 获取分类标签样式
 */
function getCategoryTagType(category: number): 'info' | 'warning' | 'success' {
  switch (category) {
    case MessageCategory.System: return 'info'
    case MessageCategory.Business: return 'warning'
    case MessageCategory.Announcement: return 'success'
    default: return 'info'
  }
}

/**
 * 格式化日期时间
 */
function formatDateTime(time: string): string {
  if (!time) return ''
  const date = new Date(time)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}
</script>

<style scoped>
.inbox-dialog {
  width: 100%;
}

/* 移除右侧固定列（操作列）与相邻列之间的分隔竖线 */
:deep(.el-table__fixed-right::before) {
  display: none !important;
}

.search-form-inline .el-form-item {
  margin-bottom: 10px;
}

.table-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.toolbar-left {
  display: flex;
  gap: 8px;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.unread-summary {
  font-size: 13px;
  color: var(--text-secondary);
}

.title-cell {
  display: flex;
  align-items: center;
  gap: 6px;
}

.title-cell.is-unread .title-text {
  font-weight: 600;
  color: var(--text-primary);
}

.unread-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--danger);
  flex-shrink: 0;
}

.title-text {
  cursor: pointer;
  color: var(--text-secondary);
}

.title-text:hover {
  color: var(--primary);
}

.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}

.message-detail {
  padding: 0 10px;
}

.detail-title {
  margin: 0 0 16px 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.detail-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  font-size: 13px;
  color: var(--text-tertiary);
  flex-wrap: wrap;
}

.detail-meta-item {
  color: var(--text-tertiary);
}

.detail-content {
  font-size: 14px;
  line-height: 1.8;
  color: #303133;
  white-space: pre-wrap;
  min-height: 100px;
}

.detail-action {
  margin-top: 20px;
  text-align: right;
}
</style>
