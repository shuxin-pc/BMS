// ==========================================
// 耗材 BOM 管理类型定义
// 需求 G2.3：耗材与服务项目通过 BOM 关联，服务完成时按 BOM 自动扣减耗材库存
// ==========================================

import type { PagedResponse } from '../shared/storeRequest'

/**
 * BOM 项（服务项目-耗材关联）
 * 对应后端实体 ServiceBom / DTO ServiceBomDto
 */
export interface BomItem {
  /** BOM 记录ID */
  id: number
  /** 服务项目ID（对应 ServiceProduct.Id） */
  serviceProductId: number
  /** 服务项目名称（冗余字段，便于展示） */
  serviceProductName: string
  /** 耗材商品ID（对应 Product.Id） */
  consumableProductId: number
  /** 耗材商品名称（冗余字段，便于展示） */
  consumableProductName: string
  /** 耗材商品编码（冗余字段，便于展示） */
  consumableProductCode?: string
  /** 单次服务消耗数量 */
  quantity: number
  /** 耗材单位（冗余字段，便于展示） */
  unit?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * BOM 查询参数（按服务项目/耗材商品筛选）
 */
export interface BomQuery {
  /** 服务项目ID（精确匹配，获取指定服务项目绑定的所有耗材） */
  serviceProductId?: number
  /** 门店商品档案ID（服务项目商品场景：BOM 服务端关联服务项目档案，后端经商品主档桥接反查） */
  productId?: number
  /** 服务项目名称（模糊匹配） */
  serviceProductName?: string
  /** 耗材商品名称（模糊匹配） */
  consumableProductName?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建 BOM 项请求
 */
export interface BomCreate {
  /** 服务项目ID */
  serviceProductId: number
  /** 耗材商品ID */
  consumableProductId: number
  /** 单次服务消耗数量 */
  quantity: number
}

/**
 * 更新 BOM 项请求
 */
export interface BomUpdate extends BomCreate {
  /** BOM 记录ID */
  id: number
}

// 复用 shared/storeRequest 的 PagedResponse，re-export 供外部使用
export type { PagedResponse }
