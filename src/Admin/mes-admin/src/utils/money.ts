/**
 * 金额计算工具：规避 JavaScript 浮点尾数误差
 * 背景：JS 二进制浮点数无法精确表示 0.1 等十进制小数，金额加减乘可能产生尾数
 * （如 210.15 - 101.05 = 109.10000000000001）。若将未规整的结果原样提交后端，
 * decimal 精度比较会失败（如退款报「退款金额超出可退金额」）、或导致统计/流水金额错位。
 * 约定：所有金额计算结果在「入库/提交后端/参与比较」前必须经 roundMoney 规整。
 */

/**
 * 金额规整到指定小数位（默认 2 位），返回规整后的数值
 * 例：roundMoney(210.15 - 101.05) -> 109.1；roundMoney(109.1, 0) -> 109
 * @param value 待规整金额
 * @param digits 保留小数位，默认 2
 * @returns 规整后的金额数值
 */
export function roundMoney(value: number, digits = 2): number {
  return Number(value.toFixed(digits))
}

/**
 * 金额相加（先求和再统一规整，避免逐项累加造成误差累积）
 * @param values 加数列表
 * @returns 规整后的和
 */
export function addMoney(...values: number[]): number {
  return roundMoney(values.reduce((sum, v) => sum + (v || 0), 0))
}

/**
 * 金额相减（a - b）
 * @param a 被减数
 * @param b 减数
 * @returns 规整后的差
 */
export function subtractMoney(a: number, b: number): number {
  return roundMoney(a - (b || 0))
}

/**
 * 金额相乘（金额 × 数量，如单价 × 数量）
 * @param a 因子1
 * @param b 因子2
 * @returns 规整后的积
 */
export function multiplyMoney(a: number, b: number): number {
  return roundMoney(a * b)
}
