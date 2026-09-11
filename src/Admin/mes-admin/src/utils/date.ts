/**
 * 日期展示格式化工具
 * 后端契约：DateTime 序列化为 ISO 8601 "yyyy-MM-ddTHH:mm:ss"
 * 统一前端日期展示格式，消除各页面重复实现的本地格式化函数：
 * - formatDate            纯日期       -> yyyy-MM-dd（如 2026-08-18）
 * - formatDateTime        日期时间     -> yyyy-MM-dd HH:mm（如 2026-08-18 16:45）
 * - formatDateTimeSeconds 含秒日期时间 -> yyyy-MM-dd HH:mm:ss（如 2026-08-18 16:45:30）
 * 空值（'' / undefined / null）与非法日期统一返回 '-'
 */

/**
 * 将入参解析为 Date 对象，空值或非法日期返回 null
 * @param date 后端 ISO 字符串、Date 实例或空值
 */
function parseDate(date?: string | Date | null): Date | null {
  if (date === null || date === undefined || date === '') return null
  const d = date instanceof Date ? date : new Date(date)
  if (Number.isNaN(d.getTime())) return null
  return d
}

/**
 * 格式化纯日期：yyyy-MM-dd
 * 例："2026-08-18T16:45:00" -> "2026-08-18"；空值/非法返回 "-"
 * @param date 日期字符串或 Date 实例
 */
export function formatDate(date?: string | Date | null): string {
  const d = parseDate(date)
  if (!d) return '-'
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/**
 * 格式化日期时间（分钟级）：yyyy-MM-dd HH:mm
 * 例："2026-08-18T16:45:30" -> "2026-08-18 16:45"；空值/非法返回 "-"
 * @param date 日期字符串或 Date 实例
 */
export function formatDateTime(date?: string | Date | null): string {
  const d = parseDate(date)
  if (!d) return '-'
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/**
 * 格式化日期时间（秒级）：yyyy-MM-dd HH:mm:ss
 * 例："2026-08-18T16:45:30" -> "2026-08-18 16:45:30"；空值/非法返回 "-"
 * 用于审计日志、库存流水等需要秒级精度的展示场景
 * @param date 日期字符串或 Date 实例
 */
export function formatDateTimeSeconds(date?: string | Date | null): string {
  const d = parseDate(date)
  if (!d) return '-'
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}
