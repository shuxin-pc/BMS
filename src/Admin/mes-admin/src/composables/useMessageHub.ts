/**
 * SignalR 消息连接 composable
 * 管理连接生命周期：登录后建立连接，登出时断开
 */
import * as signalR from '@microsoft/signalr'
import { useMessageStore } from '@/stores/message'
import type { MessageInboxItem } from '@/api/message/types'

let connection: signalR.HubConnection | null = null

export function useMessageHub() {
  /**
   * 建立 SignalR 连接
   */
  async function start() {
    if (connection) {
      return
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl('/api/system/hub/messages', {
        accessTokenFactory: () => localStorage.getItem('token') || ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build()

    connection.on('ReceiveMessage', (message: MessageInboxItem) => {
      const messageStore = useMessageStore()
      messageStore.onReceiveMessage(message)
    })

    // 重连成功后拉取未读数兜底（重连期间可能丢失推送）
    connection.onreconnected(async () => {
      const messageStore = useMessageStore()
      await messageStore.fetchUnreadCount()
    })

    try {
      await connection.start()
      console.log('[MessageHub] SignalR 连接成功')
    } catch (error) {
      console.error('[MessageHub] SignalR 连接失败:', error)
    }
  }

  /**
   * 断开 SignalR 连接
   */
  async function stop() {
    if (connection) {
      await connection.stop()
      connection = null
      console.log('[MessageHub] SignalR 连接已断开')
    }
  }

  return { start, stop }
}
