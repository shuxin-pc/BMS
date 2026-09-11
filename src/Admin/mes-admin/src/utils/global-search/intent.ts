// 全局搜索 - 意图加权规则
// 根据关键字特征识别用户搜索意图，为对应分组额外加分（叠加在分组基础权重之上）

/**
 * 识别关键字意图，返回分组 → 附加权重映射（未命中的分组不在结果中）
 * @param keyword 搜索关键字
 */
export function getIntentBoost(keyword: string): Record<string, number> {
  const boost: Record<string, number> = {}
  const kw = keyword.trim()

  // 手机号：强意图命中顾客
  if (/^1[3-9]\d{9}$/.test(kw)) {
    boost['顾客'] = 2.0
    return boost
  }
  // 纯数字（4位以上）：大概率是手机号片段/单号数字段，顾客优先
  if (/^\d{4,}$/.test(kw)) {
    boost['顾客'] = 1.3
    return boost
  }

  // 单号前缀特征（前缀见各业务单号生成器）：订单 SO / 项目卡 TC / 预约 AP
  if (/^so/i.test(kw)) boost['订单'] = 0.8
  else if (/^tc/i.test(kw)) boost['项目卡'] = 0.8
  else if (/^ap/i.test(kw)) boost['预约'] = 0.8

  return boost
}
