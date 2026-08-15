// 会员储值管理 - API服务
// 对接后端 StoredValueAccountsController / StoredValueLogsController / StoredValueRulesController
import { request, buildQuery } from '../shared/storeRequest'
import type { PagedResponse } from '../shared/storeRequest'
import type {
  MemberAccount,
  MemberAccountQuery,
  RechargeRequest,
  RechargeGiftPreview,
  RechargeRule,
  RechargeRuleQuery,
  RechargeRuleCreate,
  RechargeRuleUpdate,
  MemberTransaction,
  MemberTransactionQuery,
  TransactionType,
  StoredValueCashFlow
} from './types'

// 导出类型供外部使用
export type {
  MemberAccount,
  MemberAccountQuery,
  RechargeRequest,
  RechargeGiftPreview,
  RechargeRule,
  RechargeRuleQuery,
  RechargeRuleCreate,
  RechargeRuleUpdate,
  MemberTransaction,
  MemberTransactionQuery,
  TransactionType,
  StoredValueCashFlow,
  PagedResponse
}

// ==================== 储值账户 API ====================
// 对接后端 StoredValueAccountsController（路由 /api/store/storedValueAccounts）

/**
 * 获取储值账户分页列表
 * @param query 查询参数
 * @returns 分页储值账户列表
 */
export async function getMemberAccounts(query?: MemberAccountQuery): Promise<PagedResponse<MemberAccount>> {
  const qs = buildQuery({
    customerId: query?.customerId,
    customerName: query?.customerName,
    phone: query?.phone,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<MemberAccount>>(`/storedValueAccounts${qs}`)
}

/**
 * 储值充值
 * 对接后端 POST /api/store/storedValueAccounts/recharge，
 * 由后端按储值规则计算赠送金额、写流水并在事务内更新余额（避免前端计算余额导致并发覆盖）
 * @param data 充值请求
 */
export async function rechargeAccount(data: RechargeRequest): Promise<void> {
  await request('/storedValueAccounts/recharge', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 试算充值赠送金额
 * 与实际充值使用同一计算口径，仅用于充值弹窗实时展示
 * @param amount 充值金额
 * @returns 试算结果
 */
export async function previewRechargeGift(amount: number): Promise<RechargeGiftPreview> {
  const qs = buildQuery({ amount })
  return request<RechargeGiftPreview>(`/storedValueRules/gift-preview${qs}`)
}

// ==================== 储值规则 API ====================
// 对接后端 StoredValueRulesController（路由 /api/store/storedValueRules）

/**
 * 获取储值规则分页列表
 * @param query 查询参数
 * @returns 分页储值规则列表
 */
export async function getRechargeRules(query?: RechargeRuleQuery): Promise<PagedResponse<RechargeRule>> {
  const qs = buildQuery({
    name: query?.name,
    // buildQuery 不接受 boolean，需转为字符串
    isEnabled: query?.isEnabled === undefined ? undefined : (query.isEnabled ? 'true' : 'false'),
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<RechargeRule>>(`/storedValueRules${qs}`)
}

/**
 * 创建储值规则
 * @param data 规则信息
 * @returns 创建后的规则
 */
export async function createRechargeRule(data: RechargeRuleCreate): Promise<RechargeRule> {
  return request<RechargeRule>('/storedValueRules', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新储值规则
 * @param data 规则信息
 * @returns 更新后的规则
 */
export async function updateRechargeRule(data: RechargeRuleUpdate): Promise<RechargeRule> {
  return request<RechargeRule>(`/storedValueRules/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除储值规则
 * @param id 规则ID
 */
export async function deleteRechargeRule(id: number): Promise<void> {
  await request(`/storedValueRules/${id}`, { method: 'DELETE' })
}

// ==================== 储值流水 API ====================
// 对接后端 StoredValueLogsController（路由 /api/store/storedValueLogs）

/**
 * 获取储值流水分页列表
 * @param query 查询参数
 * @returns 分页储值流水
 */
export async function getMemberTransactions(query?: MemberTransactionQuery): Promise<PagedResponse<MemberTransaction>> {
  const qs = buildQuery({
    customerId: query?.customerId,
    type: query?.type,
    customerName: query?.customerName,
    phone: query?.phone,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<MemberTransaction>>(`/storedValueLogs${qs}`)
}

/**
 * 储值现金流统计（G7.3）
 * 对接后端 GET /api/store/storedValueLogs/cash-flow
 * @param startDate 起始日期（可选）
 * @param endDate 结束日期（可选）
 * @returns 储值现金流统计结果
 */
export async function getStoredValueCashFlow(
  startDate?: string,
  endDate?: string
): Promise<StoredValueCashFlow> {
  const qs = buildQuery({ startDate, endDate })
  return request<StoredValueCashFlow>(`/storedValueLogs/cash-flow${qs}`)
}
