/**
 * 时间展示格式化与本地时间戳生成工具
 * 后端契约：TimeSpan 序列化为 "HH:mm:ss"；DateTime? 序列化为 ISO 8601 "yyyy-MM-ddTHH:mm:ss"
 * 前端统一展示为 "HH:mm"，避免界面出现秒数与完整时间戳；向后端传时间用 toLocalDateTime 生成
 */

/**
 * 将后端 TimeSpan 字符串（HH:mm:ss）格式化为 HH:mm
 * 例："10:30:00" -> "10:30"；空值返回 "-"
 * @param time TimeSpan 字符串
 * @returns HH:mm 或 "-"
 */
export function formatTimeSpan(time?: string): string {
  if (!time) return '-'
  return time.slice(0, 5)
}

/**
 * 将后端 DateTime ISO 字符串格式化为 HH:mm（仅取时间部分，日期由预约日期字段展示）
 * 例："2026-08-15T12:00:00" -> "12:00"；空值返回 "-"
 * @param dateTime DateTime ISO 字符串
 * @returns HH:mm 或 "-"
 */
export function formatDateTimeToTime(dateTime?: string): string {
  if (!dateTime) return '-'
  return dateTime.slice(11, 16)
}

/**
 * 生成后端可解析的本地时间戳：yyyy-MM-ddTHH:mm:ss（无时区）
 * 后端 DateTime 列均为 PostgreSQL timestamp without time zone（字面本地钟点）：
 * - 不能用 toISOString()：返回 UTC，会整体偏移 8 小时（本地 14:17 → UTC 06:17）
 * - 不能用 formatDateTime/formatDateTimeSeconds：空格分隔格式 System.Text.Json 反序列化失败
 * 用于创建/更新表单向后端传本地时间（如购买时间、操作时间）
 * @param date Date 实例，默认当前时间
 * @returns yyyy-MM-ddTHH:mm:ss
 */
export function toLocalDateTime(date?: Date): string {
  const d = date || new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}
