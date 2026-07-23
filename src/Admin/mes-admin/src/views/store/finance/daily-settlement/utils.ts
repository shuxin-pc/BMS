/**
 * 金额格式化工具
 */

/**
 * 格式化为金额字符串（保留 2 位小数，带 ¥ 前缀）
 * @param value 金额数值
 * @returns 形如 "¥1,234.50" 的字符串
 */
export function formatMoney(value: number | null | undefined): string {
  if (value === null || value === undefined || isNaN(Number(value))) return '¥0.00'
  return `¥${Number(value).toFixed(2)}`
}

/**
 * 格式化金额数值（不带 ¥ 前缀，保留 2 位小数）
 * @param value 金额数值
 * @returns 形如 "1,234.50" 的字符串
 */
export function formatAmount(value: number | null | undefined): string {
  if (value === null || value === undefined || isNaN(Number(value))) return '0.00'
  return Number(value).toFixed(2)
}

/**
 * 格式化退款率（0 显示 "-"，否则显示百分比）
 * @param ratio 退款率（0~1+）
 * @returns 形如 "200.0%" 或 "-"
 */
export function formatRefundRatio(ratio: number | null | undefined): string {
  if (!ratio || ratio <= 0) return '-'
  return `${(ratio * 100).toFixed(1)}%`
}
