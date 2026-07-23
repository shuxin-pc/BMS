/**
 * 消息相关类型定义
 */

/**
 * 消息分类
 */
export enum MessageCategory {
  System = 1,
  Business = 2,
  Announcement = 3
}

/**
 * 消息目标类型
 */
export enum MessageTargetType {
  User = 1,
  Role = 2,
  Organization = 3,
  Tenant = 4,
  All = 5
}

/**
 * 消息来源类型
 */
export enum MessageSourceType {
  Auto = 1,
  Manual = 2
}

/**
 * 收件箱查询参数
 */
export interface MessageInboxQuery {
  pageIndex: number
  pageSize: number
  category?: number
  isRead?: boolean
  startTime?: string
  endTime?: string
}

/**
 * 收件箱项
 */
export interface MessageInboxItem {
  /** 接收记录ID */
  id: number
  /** 消息ID */
  messageId: number
  title: string
  content: string
  category: number
  categoryName: string
  senderName: string
  targetUrl: string
  isRead: boolean
  readTime?: string
  createdTime: string
}

/**
 * 管理端消息发送记录
 */
export interface MessageSentItem {
  id: number
  title: string
  content: string
  category: number
  categoryName: string
  sourceType: number
  sourceTypeName: string
  sourceSubsystemCode: string
  /** 来源子系统名称 */
  sourceSubsystemName: string
  targetType: number
  targetTypeName: string
  targetDesc: string
  senderId?: number
  senderName: string
  targetUrl: string
  isCrossTenant: boolean
  /** 归属租户名称 */
  tenantName: string
  isRecalled: boolean
  createdTime: string
}

/**
 * 管理端消息查询参数
 */
export interface MessageSentQuery {
  pageIndex: number
  pageSize: number
  category?: number
  sourceType?: number
  isRecalled?: boolean
  /** 租户筛选（仅超级管理员生效） */
  tenantId?: number
  startTime?: string
  endTime?: string
}

/**
 * 发送消息参数
 */
export interface MessageSendDto {
  title: string
  content: string
  category: number
  targetType: number
  targetIds: number[]
  targetTenantIds?: number[]
  targetUrl?: string
}

/**
 * 分页响应
 */
export interface PagedResponse<T> {
  list: T[]
  total: number
  pageIndex: number
  pageSize: number
}

/**
 * 未读人员名单项
 */
export interface MessageReadUser {
  userId: number
  /** 真实姓名（后端为空时填充"未实名用户"） */
  realName: string
}

/**
 * 消息已读/未读统计
 */
export interface MessageReadStats {
  /** 总接收人数（排除已删除接收记录） */
  totalCount: number
  /** 已读人数 */
  readCount: number
  /** 未读人数 */
  unreadCount: number
  /** 未读人员名单（前 50，按 userId 升序） */
  unreadList: MessageReadUser[]
}
