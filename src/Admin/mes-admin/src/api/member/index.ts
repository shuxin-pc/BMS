// 会员储值管理 - API服务
// 对接后端 StoredValueAccountsController / StoredValueLogsController / StoredValueRulesController
import { request, buildQuery } from '../shared/storeRequest'
import type { PagedResponse } from '../shared/storeRequest'
import type {
  MemberAccount,
  MemberAccountQuery,
  RechargeRequest,
  RechargeRule,
  RechargeRuleQuery,
  RechargeRuleCreate,
  RechargeRuleUpdate,
  MemberTransaction,
  MemberTransactionQuery,
  AccountStatus,
  TransactionType,
  StoredValueCashFlow
} from './types'

// 导出类型供外部使用
export type {
  MemberAccount,
  MemberAccountQuery,
  RechargeRequest,
  RechargeRule,
  RechargeRuleQuery,
  RechargeRuleCreate,
  RechargeRuleUpdate,
  MemberTransaction,
  MemberTransactionQuery,
  AccountStatus,
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
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<MemberAccount>>(`/storedValueAccounts${qs}`)
}

/**
 * 储值充值
 * 后端无专门充值接口，通过 StoredValueLogsController.Create 创建充值流水实现。
 * 先获取账户当前余额，计算前后值，再 POST 流水。
 * @param data 充值请求
 */
export async function rechargeAccount(data: RechargeRequest): Promise<void> {
  // 1. 获取账户当前状态
  const account = await request<MemberAccount>(`/storedValueAccounts/${data.accountId}`)

  const beforeBalance = account.balance
  const beforeRealBalance = account.realBalance
  const beforeGiftBalance = account.giftBalance

  // 充值：实收金额进入 realBalance，赠送金额进入 giftBalance
  const afterRealBalance = beforeRealBalance + data.amount
  const afterGiftBalance = beforeGiftBalance + data.bonusAmount
  const afterBalance = afterRealBalance + afterGiftBalance

  // 2. 创建充值流水（type=1 充值）
  await request('/storedValueLogs', {
    method: 'POST',
    body: JSON.stringify({
      customerId: account.customerId,
      type: 1,
      amount: data.amount + data.bonusAmount,
      realAmount: data.amount,
      giftAmount: data.bonusAmount,
      beforeBalance,
      afterBalance,
      realBalanceChange: data.amount,
      giftBalanceChange: data.bonusAmount,
      beforeRealBalance,
      afterRealBalance,
      beforeGiftBalance,
      afterGiftBalance,
      payMethod: data.paymentMethod,
      remark: data.remark || '充值'
    })
  })
}

/**
 * 冻结/解冻储值账户
 * 后端暂未实现此接口（StoredValueAccount 实体无 Status 字段，需后续迭代）
 * @param id 账户ID
 * @param status 目标状态：1-正常，2-冻结
 */
export async function toggleAccountStatus(_id: number, _status: number): Promise<void> {
  throw new Error('账户冻结功能开发中')
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
  const { id, ...rest } = data
  return request<RechargeRule>(`/storedValueRules/${id}`, {
    method: 'PUT',
    body: JSON.stringify(rest)
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
