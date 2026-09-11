// POS 快速开单混合结算 API 服务
// 对接后端 PosCheckoutsController（路由 /api/store/pos/checkouts）
import { request } from '../shared/storeRequest'
import type {
  PosCheckoutCreate,
  PosCheckoutResult,
  PosCheckoutOrder,
  PosCheckoutVerify,
  PosCheckoutVerifyResult,
  PosCheckoutSale
} from './types'

export type {
  PosCheckoutCreate,
  PosCheckoutResult,
  PosCheckoutOrder,
  PosCheckoutVerify,
  PosCheckoutVerifyResult,
  PosCheckoutSale
}

/**
 * 混合结算（单次调用：核销 + 建单 + 开卡 同一事务）
 * 对接后端：POST /api/store/pos/checkouts
 * 任一单据失败后端整体回滚（核销扣次/库存扣减/储值积分全部撤销），不会产生部分成功
 * @param data 混合结算请求（checkoutSessionNo + 可选的 order/verifies/sales）
 * @returns 三类单据全部成功的产出（供聚合展示）
 */
export async function posCheckout(data: PosCheckoutCreate): Promise<PosCheckoutResult> {
  return request<PosCheckoutResult>('/pos/checkouts', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}
