// 全局搜索 - 类型定义（与后端共享契约 Bms.BuildingBlocks.Core/Search/GlobalSearchDtos.cs 对齐）

/** 全局搜索结果项 */
export interface SearchResultItem {
  /** 主标题（顾客姓名 / 单号 / 菜单名等） */
  title: string
  /** 辅助信息（手机号 / 金额 / 日期等） */
  subtitle?: string
  /** 分组名：顾客 / 订单 / 商品 / 项目卡 / 储值 / 预约 / 用户（与后端 Group 一致） */
  group: string
}

/** 全局搜索结果分组（仅含命中的分组） */
export interface SearchResultGroup {
  /** 分组名 */
  group: string
  /** 该分组下的命中条目 */
  items: SearchResultItem[]
}
