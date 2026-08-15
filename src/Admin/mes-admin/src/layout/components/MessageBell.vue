<template>
  <el-popover
    v-model:visible="popoverVisible"
    placement="bottom-end"
    :width="380"
    trigger="click"
    popper-class="message-bell-popper"
  >
    <!-- 触发器：铃铛 + 未读数 -->
    <template #reference>
      <div class="header-icon-btn" title="消息">
        <el-badge :value="messageStore.unreadCount" :max="99" :hidden="messageStore.unreadCount === 0" class="notification-badge">
          <el-icon><Bell /></el-icon>
        </el-badge>
      </div>
    </template>

    <!-- 弹出内容 -->
    <div class="bell-panel">
      <div class="bell-header">
        <span class="bell-title">消息通知</span>
        <el-button
          v-if="messageStore.recentMessages.length > 0"
          link
          type="primary"
          :disabled="messageStore.unreadCount === 0"
          @click="handleMarkAllRead"
        >
          全部已读
        </el-button>
      </div>

      <div class="bell-body" v-loading="loading">
        <div v-if="messageStore.recentMessages.length === 0" class="bell-empty">
          <el-icon><BellFilled /></el-icon>
          <span>暂无消息</span>
        </div>

        <div
          v-for="msg in messageStore.recentMessages"
          :key="msg.id"
          class="bell-item"
          :class="{ 'is-unread': !msg.isRead }"
          @click="handleClickMessage(msg)"
        >
          <div class="bell-item-dot" :class="{ 'is-hidden': msg.isRead }"></div>
          <el-tag class="bell-item-category" size="small" :type="getCategoryTagType(msg.category)" effect="plain">
            {{ getCategoryShortName(msg.category) }}
          </el-tag>
          <div class="bell-item-title">{{ msg.title }}</div>
          <span class="bell-item-time">{{ formatTime(msg.createdTime) }}</span>
        </div>
      </div>

      <div class="bell-footer" v-if="messageStore.recentMessages.length > 0">
        <el-button link @click="goInbox">查看全部消息</el-button>
      </div>
    </div>
  </el-popover>

  <!-- 收件箱弹窗 -->
  <InboxDialog v-model="inboxVisible" />

  <!-- 消息详情弹窗 -->
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
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Bell, BellFilled } from '@element-plus/icons-vue'
import { useMessageStore } from '@/stores/message'
import { markAsRead, markAllAsRead } from '@/api/message'
import type { MessageInboxItem } from '@/api/message/types'
import InboxDialog from './InboxDialog.vue'

const messageStore = useMessageStore()

const popoverVisible = ref(false)
const loading = ref(false)
// 收件箱弹窗显隐
const inboxVisible = ref(false)
// 消息详情弹窗
const detailVisible = ref(false)
const currentMessage = ref<MessageInboxItem | null>(null)

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([
      messageStore.fetchUnreadCount(),
      messageStore.fetchRecentMessages()
    ])
  } finally {
    loading.value = false
  }
})

/**
 * 获取分类标签样式
 */
function getCategoryTagType(category: number): 'info' | 'warning' | 'success' {
  switch (category) {
    case 1: return 'info'       // 系统消息
    case 2: return 'warning'    // 业务通知
    case 3: return 'success'    // 公告信息
    default: return 'info'
  }
}

/**
 * 获取分类简短名称（列表标签展示）
 */
function getCategoryShortName(category: number): string {
  switch (category) {
    case 1: return '系统'
    case 2: return '通知'
    case 3: return '公告'
    default: return ''
  }
}

/**
 * 格式化时间（列表简化展示）
 */
function formatTime(time: string): string {
  if (!time) return ''
  const date = new Date(time)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const minute = 60 * 1000
  const hour = 60 * minute
  const day = 24 * hour

  if (diff < minute) return '刚刚'
  if (diff < hour) return `${Math.floor(diff / minute)}分钟前`
  if (diff < day) return `${Math.floor(diff / hour)}小时前`
  if (diff < 7 * day) return `${Math.floor(diff / day)}天前`
  return date.toLocaleDateString('zh-CN')
}

/**
 * 格式化日期时间（详情展示完整时间）
 */
function formatDateTime(time: string): string {
  if (!time) return ''
  const date = new Date(time)
  const pad = (n: number) => n.toString().padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/**
 * 点击单条消息：标记已读并打开详情弹窗（不跳转链接）
 */
async function handleClickMessage(msg: MessageInboxItem) {
  if (!msg.isRead) {
    try {
      await markAsRead(msg.id)
      messageStore.onMessageRead()
    } catch {
      ElMessage.error('标记已读失败')
      return
    }
  }

  popoverVisible.value = false
  currentMessage.value = msg
  detailVisible.value = true
}

/**
 * 全部标记已读
 */
async function handleMarkAllRead() {
  try {
    await markAllAsRead()
    messageStore.onAllRead()
    await messageStore.fetchRecentMessages()
    ElMessage.success('已全部标记为已读')
  } catch {
    ElMessage.error('操作失败')
  }
}

/**
 * 打开收件箱弹窗
 */
function goInbox() {
  popoverVisible.value = false
  inboxVisible.value = true
}
</script>

<style scoped>
.header-icon-btn {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-md);
  background: var(--bg-tertiary);
  border: 1px solid var(--border-primary);
  cursor: pointer;
  color: var(--text-tertiary);
  transition: all 0.3s ease;
}

.header-icon-btn:hover {
  background: var(--bg-hover);
  border-color: var(--primary);
  color: var(--primary);
}

.notification-badge :deep(.el-badge__content) {
  background: var(--danger);
  border: none;
}
</style>

<style>
/* Popper 全局样式（teleport 到 body，需用全局样式） */
.message-bell-popper.el-popover.el-popper {
  background: var(--bg-primary) !important;
  border: 1px solid var(--border-primary) !important;
  border-radius: var(--radius-lg) !important;
  box-shadow: var(--shadow-lg) !important;
  padding: 0 !important;
}

.message-bell-popper .bell-panel {
  display: flex;
  flex-direction: column;
  max-height: 480px;
}

.message-bell-popper .bell-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid var(--border-primary);
}

.message-bell-popper .bell-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.message-bell-popper .bell-body {
  flex: 1;
  overflow-y: auto;
  min-height: 80px;
}

.message-bell-popper .bell-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px 0;
  color: var(--text-disabled);
  font-size: 13px;
}

.message-bell-popper .bell-empty .el-icon {
  font-size: 32px;
  opacity: 0.4;
}

.message-bell-popper .bell-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  cursor: pointer;
  border-bottom: 1px solid var(--border-primary);
  transition: background 0.2s ease;
}

.message-bell-popper .bell-item:hover {
  background: var(--bg-hover);
}

.message-bell-popper .bell-item.is-unread {
  background: rgba(6, 212, 228, 0.04);
}

.message-bell-popper .bell-item-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--danger);
  flex-shrink: 0;
}

.message-bell-popper .bell-item-dot.is-hidden {
  visibility: hidden;
}

.message-bell-popper .bell-item-category {
  flex-shrink: 0;
}

.message-bell-popper .bell-item-title {
  flex: 1;
  min-width: 0;
  font-size: 13px;
  font-weight: 500;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.message-bell-popper .bell-item-time {
  font-size: 11px;
  color: var(--text-disabled);
  flex-shrink: 0;
}

.message-bell-popper .bell-footer {
  padding: 8px 16px;
  border-top: 1px solid var(--border-primary);
  text-align: center;
}
</style>

<style scoped>
/* 消息详情弹窗 */
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
</style>
