/**
 * 消息状态管理
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getInbox, getUnreadCount } from '@/api/message'
import type { MessageInboxItem } from '@/api/message/types'

export const useMessageStore = defineStore('message', () => {
  /** 未读消息数 */
  const unreadCount = ref(0)
  /** 最近未读消息（铃铛下拉用，最多5条） */
  const recentMessages = ref<MessageInboxItem[]>([])

  /**
   * 获取未读消息数
   */
  async function fetchUnreadCount() {
    try {
      unreadCount.value = await getUnreadCount()
    } catch (error) {
      console.error('[MessageStore] 获取未读数失败:', error)
    }
  }

  /**
   * 获取最近消息（铃铛下拉用）
   */
  async function fetchRecentMessages() {
    try {
      const result = await getInbox({ pageIndex: 1, pageSize: 5 })
      recentMessages.value = result.list || []
    } catch (error) {
      console.error('[MessageStore] 获取最近消息失败:', error)
    }
  }

  /**
   * SignalR 推送新消息时调用
   */
  function onReceiveMessage(message: MessageInboxItem) {
    unreadCount.value++
    recentMessages.value.unshift(message)
    if (recentMessages.value.length > 5) {
      recentMessages.value.pop()
    }
  }

  /**
   * 标记已读时调用（未读数 -1）
   */
  function onMessageRead() {
    unreadCount.value = Math.max(0, unreadCount.value - 1)
  }

  /**
   * 全部标记已读时调用
   */
  function onAllRead() {
    unreadCount.value = 0
  }

  return {
    unreadCount,
    recentMessages,
    fetchUnreadCount,
    fetchRecentMessages,
    onReceiveMessage,
    onMessageRead,
    onAllRead
  }
})
